using System.Collections.Concurrent;
using System.Text.Json;
using System.Transactions;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Teventive;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.MemoryImageStateStore.Testing;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image state store kept in JSON files on a file system in memory, whose files hold one vocab, "taberu", and one sentence, when the container is<br/>
/// built. The files' folder is given by a service of the container. Another program's changes to the files are files written or<br/>
/// deleted behind the store's back, through <see cref="IFileSystemInMemory"/>. A sentence's snapshot type has no Equals of its<br/>
/// own, which a reload doesn't need.</summary>
public class Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new TaggregateIdAsAGuid() } };
   static readonly JsonSerializerOptions OptionsOfAnotherProgram = new() { WriteIndented = false, Converters = { new TaggregateIdAsAGuid() } };
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-files-in-memory", Guid.NewGuid().ToString("N")));
   readonly IDependencyInjectionContainer _container;
   TaggregateId VocabId { get; } = new(Guid.NewGuid());
   TaggregateId SentenceId { get; } = new(Guid.NewGuid());
   bool FolderWasResolved { get; set; }
   ConcurrentQueue<string> ObservationLog { get; } = new();

   protected Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.Register(Singleton.For<TheFilesFolder>().CreatedBy(() =>
      {
         FolderWasResolved = true;
         return new TheFilesFolder(_folder);
      }));
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                 .Taggregate(() => new StoredSentence())
                                                 .Index<StoredVocab, string>(vocab => [vocab.Question])
                                                 .StoredInJsonFiles((TheFilesFolder it) => it.Folder, Options, files => files.OnAFileSystemInMemory()
                                                                                                                         .FolderOf<StoredVocab>("vocab")
                                                                                                                         .FolderOf<StoredSentence>("sentences")));
      builder.Registrar.LocalTessagingEngine(engine => engine.ObserveTevents(observe => observe.ForTevent((IStoredVocabTevent it) => ObservationLog.Enqueue(it.GetType().Name))));
      _container = builder.Build();
      WriteTheVocabsFile(VocabId, "taberu", "to eat");
      WriteTheSentencesFile("taberu yo");
   }

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   StoredVocab CommittedVocab => Reader.Get<StoredVocab>(VocabId);

   IMemoryImageStateStore MemoryImageStateStore => _container.RootResolver.Resolve<IMemoryImageStateStore>();

   ITaggregateStoreBulkReader BulkReader => _container.RootResolver.Resolve<ITaggregateStoreBulkReader>();

   IFileSystemInMemory Files => _container.RootResolver.Resolve<IFileSystemInMemory>();

   IReadOnlyList<TaggregateId> IdsFiledUnder(string question) => [.._container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>().Lookup(question).Select(it => it.Id)];

   void WaitForTheObservers() => _container.RootResolver.Resolve<ILocalTessagingEngine>().AwaitNoTessagesInFlightAsync().GetAwaiter().GetResult();

   string VocabFolder => Path.Combine(_folder.FullName, "vocab");

   ///<summary>A vocab's file in the default layout: &lt;first two hex digits of the id&gt;/&lt;id&gt;.json under the vocab folder.</summary>
   string VocabFile(TaggregateId id) => Path.Combine(VocabFolder, id.Value.ToString("N")[..2], $"{id.Value}.json");

   string SentenceFile => Path.Combine(_folder.FullName, "sentences", SentenceId.Value.ToString("N")[..2], $"{SentenceId.Value}.json");

   ///<summary>Writes a vocab's file, as the store would write it: what the store loads when it is first used or reloads.</summary>
   void WriteTheVocabsFile(TaggregateId id, string question, string meaning) => Files.WriteAllText(VocabFile(id), JsonSerializer.Serialize(new StoredVocabSnapshot(id, question, meaning), Options));

   void WriteTheSentencesFile(string text) => Files.WriteAllText(SentenceFile, JsonSerializer.Serialize(new StoredSentenceSnapshot(SentenceId, text), Options));

   ///<summary>Writes a vocab's file as another program would, not indented: the same state as <see cref="WriteTheVocabsFile"/> writes,<br/>
   /// written otherwise.</summary>
   void WriteTheVocabsFileOtherwise(TaggregateId id, string question, string meaning) =>
      Files.WriteAllText(VocabFile(id), JsonSerializer.Serialize(new StoredVocabSnapshot(id, question, meaning), OptionsOfAnotherProgram));

   static string AsTheStoreWrites(StoredVocabSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Options);

   ///<summary>The vocabs the vocab files hold.</summary>
   IReadOnlyList<StoredVocabSnapshot> StoredVocabs =>
      [..Files.Paths.Where(it => it.StartsWith(VocabFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
              .Select(it => JsonSerializer.Deserialize<StoredVocabSnapshot>(Files.ReadAllText(it), Options)!)];

   string StoredSentenceText => JsonSerializer.Deserialize<StoredSentenceSnapshot>(Files.ReadAllText(SentenceFile), Options)!.Text;

   ///<summary>Every file, sorted, so that a specification can compare them with the files it expects.</summary>
   IReadOnlyList<string> SortedPaths => [..Files.Paths.Order(StringComparer.OrdinalIgnoreCase)];

   static IReadOnlyList<string> Sorted(params string[] paths) => [..paths.Order(StringComparer.OrdinalIgnoreCase)];

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class when_the_container_is_built : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      [PCT] public void the_service_that_gives_the_files_folder_is_not_yet_resolved() => FolderWasResolved.Must().BeFalse();
   }

   public class when_the_store_is_first_used : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      public when_the_store_is_first_used()
      {
         _ = CommittedVocab;
         WaitForTheObservers();
      }

      [PCT] public void the_reader_gets_the_stored_vocab() => new { CommittedVocab.Question, CommittedVocab.Meaning }.Must().DeepEqual(new { Question = "taberu", Meaning = "to eat" });
      [PCT] public void the_index_files_the_stored_vocab_under_its_question() => IdsFiledUnder("taberu").Must().SequenceEqual([VocabId]);
      [PCT] public void no_tevent_was_dispatched() => ObservationLog.Must().BeEmpty();
      [PCT] public void the_service_that_gives_the_files_folder_has_been_resolved() => FolderWasResolved.Must().BeTrue();

      public class and_another_program_then_writes_another_vocabs_file : when_the_store_is_first_used
      {
         readonly TaggregateId _writtenAfterTheFirstUse = new(Guid.NewGuid());

         public and_another_program_then_writes_another_vocabs_file() => WriteTheVocabsFile(_writtenAfterTheFirstUse, "nomu", "to drink");

         [PCT] public void the_store_loaded_at_its_first_use_alone_so_the_reader_doesnt_find_the_vocab_before_a_reload() => Reader.TryGet(_writtenAfterTheFirstUse, out StoredVocab? _).Must().BeFalse();
      }
   }

   public class when_the_files_also_hold_five_vocabs_with_one_question_and_the_store_is_first_used : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly TaggregateId[] _ids = [new(), new(), new(), new(), new()];

      public when_the_files_also_hold_five_vocabs_with_one_question_and_the_store_is_first_used()
      {
         foreach(var id in _ids) WriteTheVocabsFile(id, "kuu", "to eat, rudely");
      }

      [PCT] public void the_lookup_gives_the_five() => IdsFiledUnder("kuu").Must().Satisfy(it => it.Count == 5 && _ids.All(it.Contains));
   }

   public class when_asked_for_every_taggregate_of_a_class : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      [PCT] public void every_vocab_is_the_stored_vocab() => BulkReader.ReferencesToAll<StoredVocab>().Select(it => it.Id).Must().SequenceEqual([VocabId]);
      [PCT] public void every_sentence_is_the_stored_sentence() => BulkReader.ReferencesToAll<StoredSentence>().Select(it => it.Id).Must().SequenceEqual([SentenceId]);
      [PCT] public void every_taggregate_is_the_vocab_and_the_sentence() => BulkReader.ReferencesToAll<ITaggregate>().Select(it => it.Id).ToList().Must().Satisfy(it => it.Count == 2 && it.Contains(VocabId) && it.Contains(SentenceId));
      [PCT] public void the_reference_to_the_stored_vocab_gives_it() => BulkReader.ReferencesToAll<StoredVocab>().Single().Current.Question.Must().Be("taberu");
   }

   public class when_a_unit_of_work_that_added_a_new_vocab_and_deleted_the_stored_one_asks_for_every_vocab : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly TaggregateId _newVocabId = new(Guid.NewGuid());
      IReadOnlyList<TaggregateId> _everyVocabInsideTheUnitOfWork = [];

      public when_a_unit_of_work_that_added_a_new_vocab_and_deleted_the_stored_one_asks_for_every_vocab() =>
         InUnitOfWork(taggregates =>
         {
            taggregates.Add(StoredVocab.Create(_newVocabId, "kuu"));
            taggregates.GetForUpdate<StoredVocab>(VocabId).Delete();
            _everyVocabInsideTheUnitOfWork = [..BulkReader.ReferencesToAll<StoredVocab>().Select(it => it.Id)];
         });

      [PCT] public void inside_the_unit_of_work_every_vocab_is_the_new_one() => _everyVocabInsideTheUnitOfWork.Must().SequenceEqual([_newVocabId]);
      [PCT] public void after_the_commit_every_vocab_is_the_new_one() => BulkReader.ReferencesToAll<StoredVocab>().Select(it => it.Id).Must().SequenceEqual([_newVocabId]);
   }

   ///<summary>Another program rewrites the sentence's file after the store's first use, so a write of the sentence by the store would<br/>
   /// put back the text the store holds.</summary>
   public class after_another_program_rewrites_the_sentences_file_and_a_unit_of_work_changes_the_vocabs_meaning : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      public after_another_program_rewrites_the_sentences_file_and_a_unit_of_work_changes_the_vocabs_meaning()
      {
         _ = CommittedVocab;
         WriteTheSentencesFile("taberu ne");
         InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine"));
      }

      [PCT] public void the_vocabs_file_holds_the_new_meaning() => StoredVocabs.Select(it => it.Meaning).Must().SequenceEqual(["to dine"]);
      [PCT] public void the_store_wrote_only_the_vocab_so_the_sentences_file_holds_what_the_other_program_wrote() => StoredSentenceText.Must().Be("taberu ne");

      public class and_the_store_reloads : after_another_program_rewrites_the_sentences_file_and_a_unit_of_work_changes_the_vocabs_meaning
      {
         readonly StoredVocab _committedBeforeTheReload;

         public and_the_store_reloads()
         {
            _committedBeforeTheReload = CommittedVocab;
            MemoryImageStateStore.LoadFromStorage();
         }

         [PCT] public void the_reader_gets_a_vocab_restored_from_its_file_not_the_instance_committed_before() => CommittedVocab.Must().NotReferenceEqual(_committedBeforeTheReload);
      }
   }

   public class after_a_unit_of_work_adds_a_new_vocab : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      public after_a_unit_of_work_adds_a_new_vocab() => InUnitOfWork(taggregates => taggregates.Add(StoredVocab.Create(new TaggregateId(Guid.NewGuid()), "kuu")));

      [PCT] public void the_vocab_files_hold_both_vocabs() => StoredVocabs.Select(it => it.Question).Order().Must().SequenceEqual(["kuu", "taberu"]);
   }

   public class after_a_unit_of_work_deletes_the_vocab : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      public after_a_unit_of_work_deletes_the_vocab() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());

      [PCT] public void no_vocab_file_is_left() => StoredVocabs.Must().BeEmpty();
      [PCT] public void the_sentences_file_alone_is_left() => SortedPaths.Must().SequenceEqual([SentenceFile]);
   }

   public class after_a_unit_of_work_adds_a_new_vocab_and_deletes_it : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly TaggregateId _newVocabId = new(Guid.NewGuid());

      public after_a_unit_of_work_adds_a_new_vocab_and_deletes_it() =>
         InUnitOfWork(taggregates =>
         {
            var vocab = StoredVocab.Create(_newVocabId, "kuu");
            taggregates.Add(vocab);
            vocab.Delete();
         });

      [PCT] public void the_files_are_the_vocabs_and_the_sentences_alone_as_before() => SortedPaths.Must().SequenceEqual(Sorted(VocabFile(VocabId), SentenceFile));
      [PCT] public void the_reader_gets_no_new_vocab() => Reader.TryGet(_newVocabId, out StoredVocab? _).Must().BeFalse();
      [PCT] public void nothing_is_filed_under_its_question() => IdsFiledUnder("kuu").Must().BeEmpty();
   }

   ///<summary>Another program rewrites the vocab's file after the store's first use, so a write of the vocab by the store would put back<br/>
   /// the meaning the store holds.</summary>
   public class after_another_program_rewrites_the_vocabs_file_and_a_unit_of_work_gets_the_vocab_for_update_and_changes_nothing : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      public after_another_program_rewrites_the_vocabs_file_and_a_unit_of_work_gets_the_vocab_for_update_and_changes_nothing()
      {
         _ = CommittedVocab;
         WriteTheVocabsFile(VocabId, "taberu", "to eat, rudely");
         InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId));
      }

      [PCT] public void the_store_wrote_nothing_so_the_vocabs_file_holds_what_the_other_program_wrote() => StoredVocabs.Select(it => it.Meaning).Must().SequenceEqual(["to eat, rudely"]);
   }

   public class when_another_participant_of_the_transaction_rolls_it_back_after_the_store_has_prepared : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      public when_another_participant_of_the_transaction_rolls_it_back_after_the_store_has_prepared() =>
         Invoking(() => InUnitOfWork(taggregates =>
                 {
                    taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine");
                    Transaction.Current!.EnlistVolatile(new ParticipantThatRollsBack(), EnlistmentOptions.None);
                 }))
           .Must().Throw<TransactionAbortedException>();

      [PCT] public void the_vocabs_file_still_holds_the_meaning_committed_before() => StoredVocabs.Select(it => it.Meaning).Must().SequenceEqual(["to eat"]);
      [PCT] public void the_prepared_write_was_discarded_so_no_file_but_the_vocabs_and_the_sentences_is_left() => SortedPaths.Must().SequenceEqual(Sorted(VocabFile(VocabId), SentenceFile));
      [PCT] public void the_reader_gets_the_meaning_committed_before() => CommittedVocab.Meaning.Must().Be("to eat");
   }

   public class after_another_program_changes_the_vocabs_file_and_writes_another_and_the_store_reloads : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly TaggregateId _addedVocabId = new(Guid.NewGuid());
      readonly ITaggregateReference<StoredVocab> _referenceHeldThroughout;
      readonly ChangesFoundInStorage _reloaded;

      public after_another_program_changes_the_vocabs_file_and_writes_another_and_the_store_reloads()
      {
         _referenceHeldThroughout = Reader.Reference<StoredVocab>(VocabId);
         _ = _referenceHeldThroughout.Current;
         WriteTheVocabsFile(VocabId, "kuu", "to eat, rudely");
         WriteTheVocabsFile(_addedVocabId, "nomu", "to drink");
         _reloaded = MemoryImageStateStore.LoadFromStorage();
         WaitForTheObservers();
      }

      [PCT] public void the_reload_reports_the_vocab_as_changed() => _reloaded.Changed.Must().SequenceEqual([VocabId]);
      [PCT] public void the_reload_reports_the_other_vocab_as_added() => _reloaded.Added.Must().SequenceEqual([_addedVocabId]);
      [PCT] public void the_reload_reports_none_removed() => _reloaded.Removed.Must().BeEmpty();
      [PCT] public void the_reader_gets_the_changed_vocab() => new { CommittedVocab.Question, CommittedVocab.Meaning }.Must().DeepEqual(new { Question = "kuu", Meaning = "to eat, rudely" });
      [PCT] public void the_reference_held_throughout_gives_the_changed_vocab() => _referenceHeldThroughout.Current.Question.Must().Be("kuu");
      [PCT] public void the_index_files_the_vocab_under_its_new_question() => IdsFiledUnder("kuu").Must().SequenceEqual([VocabId]);
      [PCT] public void the_index_files_nothing_under_its_old_question() => IdsFiledUnder("taberu").Must().BeEmpty();
      [PCT] public void no_tevent_was_dispatched() => ObservationLog.Must().BeEmpty();
   }

   public class after_another_program_changes_the_sentences_file_and_the_store_reloads : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly ChangesFoundInStorage _reloaded;

      public after_another_program_changes_the_sentences_file_and_the_store_reloads()
      {
         _ = CommittedVocab;
         WriteTheSentencesFile("taberu ne");
         _reloaded = MemoryImageStateStore.LoadFromStorage();
      }

      [PCT] public void the_reload_reports_the_sentence_as_changed_though_its_snapshot_type_has_no_Equals() => _reloaded.Changed.Must().SequenceEqual([SentenceId]);
   }

   public class after_another_program_rewrites_the_sentences_file_with_the_text_it_held_and_the_store_reloads : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly ChangesFoundInStorage _reloaded;

      public after_another_program_rewrites_the_sentences_file_with_the_text_it_held_and_the_store_reloads()
      {
         _ = CommittedVocab;
         WriteTheSentencesFile("taberu yo");
         _reloaded = MemoryImageStateStore.LoadFromStorage();
      }

      [PCT] public void the_reload_reports_nothing_changed_though_the_sentences_snapshot_type_has_no_Equals() => _reloaded.Changed.Must().BeEmpty();
   }

   public class after_another_program_rewrites_the_vocabs_file_with_the_state_the_store_holds_and_the_store_reloads : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly ChangesFoundInStorage _reloaded;

      public after_another_program_rewrites_the_vocabs_file_with_the_state_the_store_holds_and_the_store_reloads()
      {
         _ = CommittedVocab;
         WriteTheVocabsFile(VocabId, "taberu", "to eat");
         _reloaded = MemoryImageStateStore.LoadFromStorage();
      }

      [PCT] public void the_reload_reports_nothing_changed() => _reloaded.Changed.Must().BeEmpty();
      [PCT] public void the_reader_gets_the_vocab() => new { CommittedVocab.Question, CommittedVocab.Meaning }.Must().DeepEqual(new { Question = "taberu", Meaning = "to eat" });
   }

   public class after_another_program_writes_the_state_the_store_holds_in_the_vocabs_file_otherwise_and_the_store_reloads : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly ChangesFoundInStorage _reloaded;

      public after_another_program_writes_the_state_the_store_holds_in_the_vocabs_file_otherwise_and_the_store_reloads()
      {
         _ = CommittedVocab;
         WriteTheVocabsFileOtherwise(VocabId, "taberu", "to eat");
         _reloaded = MemoryImageStateStore.LoadFromStorage();
      }

      [PCT] public void the_file_holds_other_text_than_the_store_writes() => Files.ReadAllText(VocabFile(VocabId)).Must().NotBe(AsTheStoreWrites(new StoredVocabSnapshot(VocabId, "taberu", "to eat")));
      [PCT] public void the_reload_reports_nothing_changed() => _reloaded.Changed.Must().BeEmpty();
   }

   public class after_another_program_writes_the_vocabs_file_otherwise_and_the_store_writes_every_taggregate_anew : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly int _versionBefore;

      public after_another_program_writes_the_vocabs_file_otherwise_and_the_store_writes_every_taggregate_anew()
      {
         _versionBefore = CommittedVocab.Version;
         WriteTheVocabsFileOtherwise(VocabId, "taberu", "to eat");
         MemoryImageStateStore.WriteAllToStorage();
         WaitForTheObservers();
      }

      [PCT] public void the_vocabs_file_holds_exactly_what_the_consumers_options_make_of_its_snapshot() =>
         Files.ReadAllText(VocabFile(VocabId)).Must().Be(AsTheStoreWrites(new StoredVocabSnapshot(VocabId, "taberu", "to eat")));

      [PCT] public void the_sentences_file_holds_exactly_what_the_consumers_options_make_of_its_snapshot() =>
         Files.ReadAllText(SentenceFile).Must().Be(JsonSerializer.Serialize(new StoredSentenceSnapshot(SentenceId, "taberu yo"), Options));

      [PCT] public void no_file_but_the_vocabs_and_the_sentences_is_left() => SortedPaths.Must().SequenceEqual(Sorted(VocabFile(VocabId), SentenceFile));
      [PCT] public void the_reader_gets_the_vocab_as_before() => new { CommittedVocab.Question, CommittedVocab.Meaning }.Must().DeepEqual(new { Question = "taberu", Meaning = "to eat" });
      [PCT] public void the_vocabs_version_is_as_before() => CommittedVocab.Version.Must().Be(_versionBefore);
      [PCT] public void no_tevent_was_dispatched() => ObservationLog.Must().BeEmpty();

      public class and_the_store_then_reloads : after_another_program_writes_the_vocabs_file_otherwise_and_the_store_writes_every_taggregate_anew
      {
         readonly ChangesFoundInStorage _reloaded;

         public and_the_store_then_reloads() => _reloaded = MemoryImageStateStore.LoadFromStorage();

         [PCT] public void the_reload_reports_nothing_changed() => _reloaded.Changed.Must().BeEmpty();
         [PCT] public void the_reload_reports_nothing_added_or_removed() => _reloaded.Added.Concat(_reloaded.Removed).Must().BeEmpty();
      }
   }

   public class when_the_store_writes_every_taggregate_anew_while_a_unit_of_work_on_another_thread_changes_the_vocab : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly ManualResetEventSlim _theUnitOfWorkHasTheVocab = new();
      volatile bool _theWriteHasReturned;
      bool _theWriteHadReturnedWhenTheUnitOfWorkFinishedChangingTheVocab;

      public when_the_store_writes_every_taggregate_anew_while_a_unit_of_work_on_another_thread_changes_the_vocab()
      {
         //The unit of work records, as the last thing it does before it commits, whether the write has returned: a write that
         //waits for it can't have. Whether its task has completed when the write returns can't tell: the write may return between
         //the unit of work leaving the store and its task being marked completed.
         var unitOfWork = Task.Run(() => InUnitOfWork(taggregates =>
         {
            var vocab = taggregates.GetForUpdate<StoredVocab>(VocabId);
            _theUnitOfWorkHasTheVocab.Set();
            Thread.Sleep(200); //Gives the write time to begin waiting while the unit of work is open.
            vocab.ChangeMeaning("to dine");
            _theWriteHadReturnedWhenTheUnitOfWorkFinishedChangingTheVocab = _theWriteHasReturned;
         }));
         _theUnitOfWorkHasTheVocab.Wait();
         MemoryImageStateStore.WriteAllToStorage();
         _theWriteHasReturned = true;
         unitOfWork.Wait();
      }

      [PCT] public void the_write_waited_for_the_unit_of_work_to_complete() => _theWriteHadReturnedWhenTheUnitOfWorkFinishedChangingTheVocab.Must().BeFalse();
      [PCT] public void the_vocabs_file_holds_what_the_unit_of_work_committed() => StoredVocabs.Select(it => it.Meaning).Must().SequenceEqual(["to dine"]);
   }

   public class when_a_unit_of_work_that_changed_the_vocab_has_the_store_write_every_taggregate_anew : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      [PCT] public void it_is_refused() =>
         Invoking(() => InUnitOfWork(taggregates =>
         {
            taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine");
            MemoryImageStateStore.WriteAllToStorage();
         })).Must().Throw<Exception>().Which.Message.Must().Contain("can't have it write every taggregate anew");
   }

   public class after_another_program_deletes_the_vocabs_file_and_the_store_reloads : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly ChangesFoundInStorage _reloaded;

      public after_another_program_deletes_the_vocabs_file_and_the_store_reloads()
      {
         _ = CommittedVocab;
         Files.Delete(VocabFile(VocabId));
         _reloaded = MemoryImageStateStore.LoadFromStorage();
      }

      [PCT] public void the_reload_reports_the_vocab_as_removed() => _reloaded.Removed.Must().SequenceEqual([VocabId]);
      [PCT] public void the_reader_finds_no_vocab() => Reader.TryGet(VocabId, out StoredVocab? _).Must().BeFalse();
      [PCT] public void the_index_files_nothing_under_its_question() => IdsFiledUnder("taberu").Must().BeEmpty();
   }

   public class when_the_store_reloads_while_a_unit_of_work_on_another_thread_changes_the_vocab : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly ManualResetEventSlim _theUnitOfWorkHasTheVocab = new();
      readonly ChangesFoundInStorage _reloaded;

      public when_the_store_reloads_while_a_unit_of_work_on_another_thread_changes_the_vocab()
      {
         var unitOfWork = Task.Run(() => InUnitOfWork(taggregates =>
         {
            var vocab = taggregates.GetForUpdate<StoredVocab>(VocabId);
            _theUnitOfWorkHasTheVocab.Set();
            Thread.Sleep(200); //Gives the reload time to begin waiting while the unit of work is open.
            vocab.ChangeMeaning("to dine");
         }));
         _theUnitOfWorkHasTheVocab.Wait();
         _reloaded = MemoryImageStateStore.LoadFromStorage();
         unitOfWork.Wait();
      }

      [PCT] public void the_reload_waited_for_the_unit_of_work_so_it_reports_no_change() => _reloaded.Changed.Must().BeEmpty();
      [PCT] public void the_reader_gets_what_the_unit_of_work_committed() => CommittedVocab.Meaning.Must().Be("to dine");
   }

   public class after_the_store_reloads_after_a_change_that_rewrites_the_vocabs_file : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly ChangesFoundInStorage _reloaded;

      public after_the_store_reloads_after_a_change_that_rewrites_the_vocabs_file()
      {
         _ = CommittedVocab;
         _reloaded = MemoryImageStateStore.LoadFromStorage(() => WriteTheVocabsFile(VocabId, "kuu", "to eat, rudely"));
      }

      [PCT] public void the_reload_reports_the_vocab_as_changed() => _reloaded.Changed.Must().SequenceEqual([VocabId]);
      [PCT] public void the_reader_gets_the_changed_vocab() => CommittedVocab.Question.Must().Be("kuu");
   }

   public class when_a_unit_of_work_on_another_thread_begins_while_the_change_before_a_reload_runs : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      public when_a_unit_of_work_on_another_thread_begins_while_the_change_before_a_reload_runs()
      {
         _ = CommittedVocab;
         Task? unitOfWork = null;
         MemoryImageStateStore.LoadFromStorage(() =>
         {
            WriteTheVocabsFile(VocabId, "kuu", "to eat, rudely");
            unitOfWork = Task.Run(() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine")));
            Thread.Sleep(200); //Gives the unit of work time to begin, so that it would write the vocab committed before over the change, did it not wait.
         });
         unitOfWork!.Wait();
      }

      [PCT] public void the_unit_of_work_changed_the_vocab_as_reloaded_so_its_file_keeps_the_change() =>
         StoredVocabs.Select(it => new { it.Question, it.Meaning }).Must().SequenceEqual([new { Question = "kuu", Meaning = "to dine" }]);
   }

   public class when_the_change_before_a_reload_throws : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      readonly Exception _exception;

      public when_the_change_before_a_reload_throws()
      {
         _ = CommittedVocab;
         _exception = Invoking(() => MemoryImageStateStore.LoadFromStorage(() =>
                                    {
                                       WriteTheVocabsFile(VocabId, "kuu", "to eat, rudely");
                                       throw new InvalidOperationException("The change fails.");
                                    }))
                     .Must().Throw<InvalidOperationException>().Which;
      }

      [PCT] public void the_changes_exception_comes_out() => _exception.Message.Must().Be("The change fails.");
      [PCT] public void nothing_is_reloaded() => CommittedVocab.Question.Must().Be("taberu");

      public class and_a_unit_of_work_then_changes_the_vocab : when_the_change_before_a_reload_throws
      {
         public and_a_unit_of_work_then_changes_the_vocab() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine"));

         [PCT] public void the_reader_gets_the_change() => CommittedVocab.Meaning.Must().Be("to dine");
      }
   }

   public class when_a_unit_of_work_that_changed_the_vocab_reloads_the_store : Given_a_memory_image_state_store_whose_files_hold_a_vocab_and_a_sentence
   {
      [PCT] public void the_reload_throws() =>
         Invoking(() => InUnitOfWork(taggregates =>
         {
            taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine");
            MemoryImageStateStore.LoadFromStorage();
         })).Must().Throw<Exception>();
   }

   ///<summary>A service of the container that knows the files' folder, as a host that learns its paths once it has started does.</summary>
   sealed class TheFilesFolder
   {
      internal TheFilesFolder(DirectoryInfo folder) => Folder = folder;

      internal DirectoryInfo Folder { get; }
   }

   ///<summary>A transaction participant that votes to roll back. It prepares after the store, which prepares in the transaction's<br/>
   /// first phase.</summary>
   class ParticipantThatRollsBack : IEnlistmentNotification
   {
      public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.ForceRollback();
      public void Commit(Enlistment enlistment) => enlistment.Done();
      public void Rollback(Enlistment enlistment) => enlistment.Done();
      public void InDoubt(Enlistment enlistment) => enlistment.Done();
   }
}
