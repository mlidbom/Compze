using System.Text;
using System.Text.Json;
using System.Transactions;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.StateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>A state store keeping vocabs and sentences in JSON files, in a fresh folder of its own: vocabs in "vocab", sentences in<br/>
/// "sentences", each in the default layout unless a context declares the vocabs' files. Each store started is a container of its own<br/>
/// on the same folder, as each start of an application is.</summary>
public class Given_a_state_store_kept_in_files : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new TaggregateIdAsAGuid() } };
   readonly List<IDependencyInjectionContainer> _containers = [];
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-in-files", Guid.NewGuid().ToString("N")));
   Func<StoredVocab, DirectoryInfo, FileInfo>? _vocabFileOf;
   bool _folderGivenByAService;
   IDependencyInjectionContainer? _store;

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   DirectoryInfo VocabFolder => new(Path.Combine(_folder.FullName, "vocab"));

   string StoresOwnFolder => Path.Combine(_folder.FullName, ".state-store");

   ///<summary>The vocab's file in the default layout: &lt;first two hex digits of the id&gt;/&lt;id&gt;.json under the vocab folder.</summary>
   string VocabFileByDefault => Path.Combine(VocabFolder.FullName, VocabId.Value.ToString("N")[..2], $"{VocabId.Value}.json");

   ///<summary>The store last started, starting one if none is.</summary>
   IDependencyInjectionContainer Store => _store ??= StartAStore();

   ITaggregateStoreReader Reader => Store.RootResolver.Resolve<ITaggregateStoreReader>();

   IDependencyInjectionContainer StartAStore()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.Register(Singleton.For<TheStoresFolder>().CreatedBy(() => new TheStoresFolder(_folder)));
      builder.Registrar.StateStore(store =>
      {
         store.Taggregate(() => new StoredVocab())
              .Taggregate(() => new StoredSentence());
         if(_folderGivenByAService) store.StoredInFiles((TheStoresFolder it) => it.Folder, Options, DeclareFiles);
         else store.StoredInFiles(_folder, Options, DeclareFiles);
      });
      builder.Registrar.LocalTessagingEngine(_ => {});
      var container = builder.Build();
      _containers.Add(container);
      return _store = container;
   }

   void DeclareFiles(TaggregateFiles files) => files.Of("vocab", _vocabFileOf)
                                                   .Of<StoredSentence>("sentences");

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      Store.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   void SaveTheVocab(string question) => InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, question)));

   ///<summary>Every file under the vocab folder, at any depth, as paths relative to it.</summary>
   IReadOnlyList<string> VocabFiles => VocabFolder.Exists ? [..Directory.EnumerateFiles(VocabFolder.FullName, "*", SearchOption.AllDirectories).Select(it => Path.GetRelativePath(VocabFolder.FullName, it))] : [];

   IReadOnlyList<string> FilesInTheStoresOwnFolder => Directory.Exists(StoresOwnFolder) ? [..new DirectoryInfo(StoresOwnFolder).EnumerateFiles().Select(it => it.Name)] : [];

   static string SerializedVocab(TaggregateId id, string question, string meaning) => JsonSerializer.Serialize(new StoredVocabSnapshot(id, question, meaning), Options);

   protected override async Task DisposeAsyncInternal()
   {
      foreach(var container in _containers) await container.DisposeAsync();
      if(_folder.Exists) _folder.Delete(recursive: true);
   }

   public class after_a_unit_of_work_saves_a_vocab : Given_a_state_store_kept_in_files
   {
      public after_a_unit_of_work_saves_a_vocab() => SaveTheVocab("taberu");

      [PCT] public void the_vocab_folder_holds_one_file_named_by_the_vocabs_id_in_a_folder_named_by_its_first_two_hex_digits() =>
         VocabFiles.Must().SequenceEqual([Path.GetRelativePath(VocabFolder.FullName, VocabFileByDefault)]);

      [PCT] public void the_file_holds_exactly_what_the_consumers_serializer_options_make_of_the_vocabs_snapshot_in_utf8_without_a_byte_order_mark() =>
         File.ReadAllBytes(VocabFileByDefault).Must().SequenceEqual(Encoding.UTF8.GetBytes(SerializedVocab(VocabId, "taberu", "")));

      [PCT] public void the_stores_own_folder_holds_no_temporary_file_or_journal() => FilesInTheStoresOwnFolder.Must().BeEmpty();

      public class and_another_store_is_started_on_the_folder : after_a_unit_of_work_saves_a_vocab
      {
         public and_another_store_is_started_on_the_folder() => StartAStore();

         [PCT] public void it_has_the_vocab_with_its_question() => Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }

      ///<summary>The store started on an empty folder, so the vocab folder didn't exist when it first loaded.</summary>
      public class and_the_store_reloads_from_its_files : after_a_unit_of_work_saves_a_vocab
      {
         readonly ReloadedTaggregates _reloaded;

         public and_the_store_reloads_from_its_files() => _reloaded = Store.RootResolver.Resolve<IStateStore>().ReloadFromStorage();

         [PCT] public void the_reload_reports_nothing_removed() => _reloaded.Removed.Must().BeEmpty();

         [PCT] public void the_store_still_has_the_vocab() => Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }

      public class and_a_unit_of_work_changes_its_meaning : after_a_unit_of_work_saves_a_vocab
      {
         public and_a_unit_of_work_changes_its_meaning() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat"));

         [PCT] public void the_file_holds_the_new_meaning() => File.ReadAllText(VocabFileByDefault).Must().Be(SerializedVocab(VocabId, "taberu", "to eat"));
      }

      public class and_a_unit_of_work_deletes_it : after_a_unit_of_work_saves_a_vocab
      {
         public and_a_unit_of_work_deletes_it() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());

         [PCT] public void its_file_is_gone() => File.Exists(VocabFileByDefault).Must().BeFalse();

         [PCT] public void another_store_started_on_the_folder_doesnt_have_it()
         {
            StartAStore();
            Reader.TryGet(VocabId, out StoredVocab? _).Must().BeFalse();
         }
      }

      public class and_a_unit_of_work_changing_it_rolls_back_after_the_store_has_prepared_its_write : after_a_unit_of_work_saves_a_vocab
      {
         public and_a_unit_of_work_changing_it_rolls_back_after_the_store_has_prepared_its_write() =>
            Invoking(() => InUnitOfWork(taggregates =>
                    {
                       taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat");
                       Transaction.Current!.EnlistVolatile(new ParticipantThatRollsBack(), EnlistmentOptions.None);
                    }))
              .Must().Throw<TransactionAbortedException>();

         [PCT] public void the_file_still_holds_the_meaning_committed_before() => File.ReadAllText(VocabFileByDefault).Must().Be(SerializedVocab(VocabId, "taberu", ""));

         [PCT] public void the_stores_own_folder_holds_no_temporary_file() => FilesInTheStoresOwnFolder.Must().BeEmpty();
      }
   }

   public class whose_folder_a_service_of_the_container_gives : Given_a_state_store_kept_in_files
   {
      protected whose_folder_a_service_of_the_container_gives() => _folderGivenByAService = true;

      public class after_a_unit_of_work_saves_a_vocab_there : whose_folder_a_service_of_the_container_gives
      {
         public after_a_unit_of_work_saves_a_vocab_there() => SaveTheVocab("taberu");

         [PCT] public void the_vocab_is_in_its_file_under_the_folder_the_service_gives() =>
            VocabFiles.Must().SequenceEqual([Path.GetRelativePath(VocabFolder.FullName, VocabFileByDefault)]);
      }
   }

   public class whose_vocabs_files_are_named_by_their_question : Given_a_state_store_kept_in_files
   {
      protected whose_vocabs_files_are_named_by_their_question() => _vocabFileOf = (vocab, folder) => new FileInfo(Path.Combine(folder.FullName, $"{vocab.Question}.json"));

      public class after_a_unit_of_work_saves_the_vocab_taberu : whose_vocabs_files_are_named_by_their_question
      {
         public after_a_unit_of_work_saves_the_vocab_taberu() => SaveTheVocab("taberu");

         [PCT] public void the_vocab_folder_holds_the_file_named_by_its_question() => VocabFiles.Must().SequenceEqual(["taberu.json"]);

         [PCT] public void another_store_started_on_the_folder_has_the_vocab()
         {
            StartAStore();
            Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
         }

         public class and_a_unit_of_work_changes_its_question : after_a_unit_of_work_saves_the_vocab_taberu
         {
            public and_a_unit_of_work_changes_its_question() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeQuestion("nomu"));

            [PCT] public void the_file_moves_to_the_name_of_the_new_question() => VocabFiles.Must().SequenceEqual(["nomu.json"]);
         }

         public class and_a_unit_of_work_saves_another_vocab_with_the_same_question : after_a_unit_of_work_saves_the_vocab_taberu
         {
            readonly Exception _exception;

            public and_a_unit_of_work_saves_another_vocab_with_the_same_question() =>
               _exception = Invoking(() => InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(new TaggregateId(Guid.NewGuid()), "taberu"))))
                           .Must().Throw<TransactionAbortedException>().Which;

            [PCT] public void the_unit_of_work_fails_naming_the_file() => _exception.InnerException!.Message.Must().Contain("taberu.json");

            [PCT] public void the_vocab_folder_still_holds_only_the_first_vocabs_file() => VocabFiles.Must().SequenceEqual(["taberu.json"]);

            [PCT] public void the_stores_own_folder_holds_no_temporary_file() => FilesInTheStoresOwnFolder.Must().BeEmpty();
         }
      }

      public class when_a_unit_of_work_saves_two_vocabs_with_the_same_question : whose_vocabs_files_are_named_by_their_question
      {
         readonly Exception _exception;

         public when_a_unit_of_work_saves_two_vocabs_with_the_same_question() =>
            _exception = Invoking(() => InUnitOfWork(taggregates =>
                         {
                            taggregates.Save(StoredVocab.Create(VocabId, "taberu"));
                            taggregates.Save(StoredVocab.Create(new TaggregateId(Guid.NewGuid()), "taberu"));
                         }))
                        .Must().Throw<TransactionAbortedException>().Which;

         [PCT] public void the_unit_of_work_fails_naming_the_file() => _exception.InnerException!.Message.Must().Contain("taberu.json");

         [PCT] public void nothing_is_written() => VocabFiles.Must().BeEmpty();
      }
   }

   public class whose_vocabs_file_is_declared_outside_the_vocab_folder : Given_a_state_store_kept_in_files
   {
      readonly Exception _exception;

      public whose_vocabs_file_is_declared_outside_the_vocab_folder()
      {
         _vocabFileOf = (vocab, folder) => new FileInfo(Path.Combine(folder.Parent!.FullName, $"{vocab.Question}.json"));
         _exception = Invoking(() => SaveTheVocab("taberu")).Must().Throw<TransactionAbortedException>().Which;
      }

      [PCT] public void saving_a_vocab_fails_naming_the_file() => _exception.InnerException!.Message.Must().Contain("taberu.json");
   }

   public class when_a_crash_left_a_journal_and_the_temporary_file_it_renames : Given_a_state_store_kept_in_files
   {
      public when_a_crash_left_a_journal_and_the_temporary_file_it_renames()
      {
         Directory.CreateDirectory(StoresOwnFolder);
         File.WriteAllText(Path.Combine(StoresOwnFolder, "1.being-written"), SerializedVocab(VocabId, "taberu", "to eat"));
         var journal = new { Renames = new[] { new { From = Path.Combine(".state-store", "1.being-written"), To = Path.GetRelativePath(_folder.FullName, VocabFileByDefault) } }, Deletions = Array.Empty<string>() };
         File.WriteAllText(Path.Combine(StoresOwnFolder, "1.journal"), JsonSerializer.Serialize(journal));
      }

      [PCT] public void a_store_started_on_the_folder_has_the_vocab() => Reader.Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");

      [PCT] public void once_the_store_has_started_the_vocabs_file_is_in_place()
      {
         _ = Reader.Get<StoredVocab>(VocabId);
         File.ReadAllText(VocabFileByDefault).Must().Be(SerializedVocab(VocabId, "taberu", "to eat"));
      }

      [PCT] public void once_the_store_has_started_its_own_folder_holds_no_file()
      {
         _ = Reader.Get<StoredVocab>(VocabId);
         FilesInTheStoresOwnFolder.Must().BeEmpty();
      }
   }

   public class when_a_crash_left_temporary_files_and_no_journal : Given_a_state_store_kept_in_files
   {
      public when_a_crash_left_temporary_files_and_no_journal()
      {
         Directory.CreateDirectory(StoresOwnFolder);
         File.WriteAllText(Path.Combine(StoresOwnFolder, "1.being-written"), SerializedVocab(VocabId, "taberu", "to eat"));
         File.WriteAllText(Path.Combine(StoresOwnFolder, "1.journal-being-written"), "{\"Renames\":[");
      }

      [PCT] public void a_store_started_on_the_folder_doesnt_have_the_vocab() => Reader.TryGet(VocabId, out StoredVocab? _).Must().BeFalse();

      [PCT] public void once_the_store_has_started_its_own_folder_holds_no_file()
      {
         _ = Reader.TryGet(VocabId, out StoredVocab? _);
         FilesInTheStoresOwnFolder.Must().BeEmpty();
      }
   }

   public class when_a_vocabs_file_isnt_json : Given_a_state_store_kept_in_files
   {
      public when_a_vocabs_file_isnt_json()
      {
         Directory.CreateDirectory(Path.GetDirectoryName(VocabFileByDefault)!);
         File.WriteAllText(VocabFileByDefault, "not json");
      }

      [PCT] public void the_store_fails_at_its_first_use_naming_the_file() =>
         Invoking(() => Reader.TryGet(VocabId, out StoredVocab? _)).Must().Throw<InvalidOperationException>().Which.Message.Must().Contain(VocabFileByDefault);
   }

   public class when_two_files_hold_one_vocab : Given_a_state_store_kept_in_files
   {
      readonly string _otherFile;

      public when_two_files_hold_one_vocab()
      {
         _otherFile = Path.Combine(VocabFolder.FullName, "copy.json");
         Directory.CreateDirectory(Path.GetDirectoryName(VocabFileByDefault)!);
         File.WriteAllText(VocabFileByDefault, SerializedVocab(VocabId, "taberu", ""));
         File.WriteAllText(_otherFile, SerializedVocab(VocabId, "taberu", ""));
      }

      [PCT] public void the_store_fails_at_its_first_use_naming_both_files() =>
         Invoking(() => Reader.TryGet(VocabId, out StoredVocab? _)).Must().Throw<InvalidOperationException>().Which.Message.Must().Satisfy(it => it.Contains(VocabFileByDefault, StringComparison.Ordinal) && it.Contains(_otherFile, StringComparison.Ordinal));
   }

   public class when_composed : Given_a_state_store_kept_in_files
   {
      static void Compose(Action<StateStoreBuilder> declare) => TestEnv.DIContainer.CreateTestingContainerBuilder().Registrar.StateStore(declare);

      [PCT] public void without_the_files_of_a_class_it_keeps_fails_naming_the_class() =>
         Invoking(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                              .Taggregate(() => new StoredSentence())
                                              .StoredInFiles(_folder, Options, files => files.Of<StoredVocab>("vocab"))))
           .Must().Throw<InvalidOperationException>().Which.Message.Must().Contain(nameof(StoredSentence));

      [PCT] public void with_two_classes_in_one_folder_fails() =>
         Invoking(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                              .Taggregate(() => new StoredSentence())
                                              .StoredInFiles(_folder, Options, files => files.Of<StoredVocab>("notes").Of<StoredSentence>("notes"))))
           .Must().Throw<InvalidOperationException>();

      [PCT] public void with_a_classs_folder_inside_another_classs_fails() =>
         Invoking(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                              .Taggregate(() => new StoredSentence())
                                              .StoredInFiles(_folder, Options, files => files.Of<StoredVocab>("notes").Of<StoredSentence>("notes/sentences"))))
           .Must().Throw<InvalidOperationException>();

      [PCT] public void with_the_stores_own_folder_as_a_classs_folder_fails() =>
         Invoking(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                              .StoredInFiles(_folder, Options, files => files.Of<StoredVocab>(".state-store"))))
           .Must().Throw<ArgumentException>();
   }

   ///<summary>A participant of the transaction that rolls it back when it prepares, after the store, which enlisted first, has.</summary>
   sealed class ParticipantThatRollsBack : IEnlistmentNotification
   {
      public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.ForceRollback();
      public void Commit(Enlistment enlistment) => enlistment.Done();
      public void Rollback(Enlistment enlistment) => enlistment.Done();
      public void InDoubt(Enlistment enlistment) => enlistment.Done();
   }

   ///<summary>A service of the container that knows the store's folder, as a host that learns its paths once it has started does.</summary>
   sealed class TheStoresFolder
   {
      internal TheStoresFolder(DirectoryInfo folder) => Folder = folder;

      internal DirectoryInfo Folder { get; }
   }
}
