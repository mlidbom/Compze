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
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image state store keeping vocabs and sentences in JSON files, in a fresh folder of its own: vocabs in "vocab", sentences in<br/>
/// "sentences", each taggregate's file &lt;first two hex digits of the id&gt;/&lt;id&gt;.json in its class's folder. Each store started is a<br/>
/// container of its own on the same folder, as each start of an application is.</summary>
public class Given_a_memory_image_state_store_kept_in_files : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new TaggregateIdAsAGuid() } };
   static readonly JsonSerializerOptions OptionsOfAnotherProgram = new() { WriteIndented = false, Converters = { new TaggregateIdAsAGuid() } };
   readonly List<IDependencyInjectionContainer> _containers = [];
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-in-files", Guid.NewGuid().ToString("N")));
   bool _folderGivenByAService;
   IDependencyInjectionContainer? _store;

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   DirectoryInfo VocabFolder => new(Path.Combine(_folder.FullName, "vocab"));

   string StoresOwnFolder => Path.Combine(_folder.FullName, ".state-store");

   ///<summary>The vocab's file: &lt;first two hex digits of the id&gt;/&lt;id&gt;.json under the vocab folder.</summary>
   string VocabFileByDefault => Path.Combine(VocabFolder.FullName, VocabId.Value.ToString("N")[..2], $"{VocabId.Value}.json");

   ///<summary>The store last started, starting one if none is.</summary>
   IDependencyInjectionContainer Store => _store ??= StartAStore();

   ITaggregateStoreReader Reader => Store.RootResolver.Resolve<ITaggregateStoreReader>();

   ///<summary>Disposes the store started last, if any, as a process does as it ends, and starts another on the folder, as the next process<br/>
   /// does: one memory image at a time uses a store's files.</summary>
   IDependencyInjectionContainer StartAStore()
   {
      if(_store != null)
      {
         _store.Dispose();
         _containers.Remove(_store);
      }

      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.Register(Singleton.For<TheStoresFolder>().CreatedBy(() => new TheStoresFolder(_folder)));
      builder.Registrar.MemoryImageStateStore(store =>
      {
         store.Taggregate(() => new StoredVocab())
              .Taggregate(() => new StoredSentence());
         if(_folderGivenByAService) store.StoredInJsonFiles((TheStoresFolder it) => it.Folder, Options, DeclareFiles);
         else store.StoredInJsonFiles(_folder, Options, DeclareFiles);
      });
      builder.Registrar.LocalTessagingEngine(_ => {});
      var container = builder.Build();
      _containers.Add(container);
      return _store = container;
   }

   static void DeclareFiles(TaggregateFilesBuilder files) => files.FolderOf<StoredVocab>("vocab")
                                                          .FolderOf<StoredSentence>("sentences");

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      Store.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   void AddTheVocab(string question) => InUnitOfWork(taggregates => taggregates.Add(StoredVocab.Create(VocabId, question)));

   ///<summary>Every file under the vocab folder, at any depth, as paths relative to it.</summary>
   IReadOnlyList<string> VocabFiles => VocabFolder.Exists ? [..Directory.EnumerateFiles(VocabFolder.FullName, "*", SearchOption.AllDirectories).Select(it => Path.GetRelativePath(VocabFolder.FullName, it))] : [];

   ///<summary>Every file in the store's own folder but its lock's.</summary>
   IReadOnlyList<string> FilesInTheStoresOwnFolder => Directory.Exists(StoresOwnFolder) ? [..new DirectoryInfo(StoresOwnFolder).EnumerateFiles().Select(it => it.Name).Where(it => !IsOfTheStoresLock(it))] : [];

   ///<summary>Whether <paramref name="fileName"/> names a file of the store's lock, which its own folder holds while a store has loaded: the lock<br/>
   /// file, and on Unix the file beside it in which its holder records itself.</summary>
   static bool IsOfTheStoresLock(string fileName) => fileName is "memory-image-state-store.lock" or "memory-image-state-store.holder";

   ///<summary>Whether the specifications run on Unix, where another program can replace the store's own folder while the store holds the lock<br/>
   /// in it: Windows refuses to delete a file that is open, as the lock is.</summary>
   public static bool OnUnix => !OperatingSystem.IsWindows();

   const string OnlyUnixLetsAProgramReplaceTheFolderOfAHeldLock = "Only on Unix can a program replace the store's own folder while the store holds the lock in it.";

   static string SerializedVocab(TaggregateId id, string question, string meaning) => JsonSerializer.Serialize(new StoredVocabSnapshot(id, question, meaning), Options);

   protected override async Task DisposeAsyncInternal()
   {
      foreach(var container in _containers) await container.DisposeAsync();
      if(_folder.Exists) _folder.Delete(recursive: true);
   }

   public class after_a_unit_of_work_adds_a_vocab : Given_a_memory_image_state_store_kept_in_files
   {
      public after_a_unit_of_work_adds_a_vocab() => AddTheVocab("taberu");

      [PCT] public void the_vocab_folder_holds_one_file_named_by_the_vocabs_id_in_a_folder_named_by_its_first_two_hex_digits() =>
         VocabFiles.Must().SequenceEqual([Path.GetRelativePath(VocabFolder.FullName, VocabFileByDefault)]);

      [PCT] public void the_file_holds_exactly_what_the_consumers_serializer_options_make_of_the_vocabs_snapshot_in_utf8_without_a_byte_order_mark() =>
         File.ReadAllBytes(VocabFileByDefault).Must().SequenceEqual(Encoding.UTF8.GetBytes(SerializedVocab(VocabId, "taberu", "")));

      [PCT] public void the_stores_own_folder_holds_no_temporary_file_or_journal() => FilesInTheStoresOwnFolder.Must().BeEmpty();

      public class and_another_store_is_started_on_the_folder : after_a_unit_of_work_adds_a_vocab
      {
         public and_another_store_is_started_on_the_folder() => StartAStore();

         [PCT] public void it_has_the_vocab_with_its_question() => Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }

      ///<summary>The store started on an empty folder, so the vocab folder didn't exist when it first loaded.</summary>
      public class and_the_store_reloads_from_its_files : after_a_unit_of_work_adds_a_vocab
      {
         readonly ChangesFoundInStorage _reloaded;

         public and_the_store_reloads_from_its_files() => _reloaded = Store.RootResolver.Resolve<IMemoryImageStateStore>().LoadFromStorage();

         [PCT] public void the_reload_reports_nothing_removed() => _reloaded.Removed.Must().BeEmpty();

         [PCT] public void the_store_still_has_the_vocab() => Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }

      public class and_a_unit_of_work_changes_its_meaning : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_changes_its_meaning() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat"));

         [PCT] public void the_file_holds_the_new_meaning() => File.ReadAllText(VocabFileByDefault).Must().Be(SerializedVocab(VocabId, "taberu", "to eat"));

         public class and_the_store_then_reloads_from_its_files : and_a_unit_of_work_changes_its_meaning
         {
            readonly ChangesFoundInStorage _reloaded;

            public and_the_store_then_reloads_from_its_files() => _reloaded = Store.RootResolver.Resolve<IMemoryImageStateStore>().LoadFromStorage();

            [PCT] public void the_reload_reports_nothing_changed() => _reloaded.Changed.Must().BeEmpty();
         }
      }

      public class and_another_program_writes_its_file_otherwise_and_the_store_writes_every_taggregate_anew : after_a_unit_of_work_adds_a_vocab
      {
         public and_another_program_writes_its_file_otherwise_and_the_store_writes_every_taggregate_anew()
         {
            File.WriteAllText(VocabFileByDefault, JsonSerializer.Serialize(new StoredVocabSnapshot(VocabId, "taberu", ""), OptionsOfAnotherProgram));
            Store.RootResolver.Resolve<IMemoryImageStateStore>().WriteAllToStorage();
         }

         [PCT] public void the_file_again_holds_exactly_what_the_consumers_serializer_options_make_of_the_vocabs_snapshot_in_utf8_without_a_byte_order_mark() =>
            File.ReadAllBytes(VocabFileByDefault).Must().SequenceEqual(Encoding.UTF8.GetBytes(SerializedVocab(VocabId, "taberu", "")));

         [PCT] public void the_stores_own_folder_again_holds_no_temporary_file_or_journal() => FilesInTheStoresOwnFolder.Must().BeEmpty();

         [PCT] public void another_store_started_on_the_folder_has_the_vocab_as_before()
         {
            StartAStore();
            Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
         }
      }

      public class and_a_unit_of_work_deletes_it : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_deletes_it() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());

         [PCT] public void its_file_is_gone() => File.Exists(VocabFileByDefault).Must().BeFalse();

         [PCT] public void another_store_started_on_the_folder_doesnt_have_it()
         {
            StartAStore();
            Reader.TryGet(VocabId, out StoredVocab? _).Must().BeFalse();
         }
      }

      public class and_a_unit_of_work_changing_it_rolls_back_after_the_store_has_prepared_its_write : after_a_unit_of_work_adds_a_vocab
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

      ///<summary>Another program puts a file where the store's own folder is, as it can on a consumer's disk, so the store can't write the<br/>
      /// temporary file of a change when the unit of work prepares. Only on Unix: on Windows, the lock the store holds in the folder keeps<br/>
      /// any program from deleting it. On Windows nothing is set up, and every specification is skipped.</summary>
      public class and_another_program_puts_a_file_where_the_stores_own_folder_is_so_a_unit_of_work_changing_it_fails_to_prepare_its_write : after_a_unit_of_work_adds_a_vocab
      {
         readonly Exception? _exception;

         public and_another_program_puts_a_file_where_the_stores_own_folder_is_so_a_unit_of_work_changing_it_fails_to_prepare_its_write()
         {
            if(!OnUnix) return;
            Directory.Delete(StoresOwnFolder, recursive: true);
            File.WriteAllText(StoresOwnFolder, "");
            _exception = Invoking(() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat")))
                        .Must().Throw<TransactionAbortedException>().Which;
         }

         [PCT(Skip = OnlyUnixLetsAProgramReplaceTheFolderOfAHeldLock, SkipType = typeof(Given_a_memory_image_state_store_kept_in_files), SkipUnless = nameof(OnUnix))]
         public void the_unit_of_work_rolls_back_with_the_disks_exception() => _exception!.InnerException.Must().BeAssignableTo<IOException>();

         [PCT(Skip = OnlyUnixLetsAProgramReplaceTheFolderOfAHeldLock, SkipType = typeof(Given_a_memory_image_state_store_kept_in_files), SkipUnless = nameof(OnUnix))]
         public void the_reader_gets_the_meaning_committed_before() => Reader.Get<StoredVocab>(VocabId).Meaning.Must().Be("");

         [PCT(Skip = OnlyUnixLetsAProgramReplaceTheFolderOfAHeldLock, SkipType = typeof(Given_a_memory_image_state_store_kept_in_files), SkipUnless = nameof(OnUnix))]
         public void the_file_still_holds_the_meaning_committed_before() => File.ReadAllText(VocabFileByDefault).Must().Be(SerializedVocab(VocabId, "taberu", ""));
      }

      ///<summary>The files are a transaction's one durable participant, so a transaction that has another, as a SQL Server connection<br/>
      /// is, would have to become distributed, which .NET refuses.</summary>
      public class and_a_unit_of_work_changing_it_holds_another_durable_participant_as_a_SQL_Server_connection_would_be : after_a_unit_of_work_adds_a_vocab
      {
         readonly Exception _exception;

         public and_a_unit_of_work_changing_it_holds_another_durable_participant_as_a_SQL_Server_connection_would_be() =>
            _exception = Invoking(() => InUnitOfWork(taggregates =>
                         {
                            Transaction.Current!.EnlistDurable(Guid.NewGuid(), new DurableParticipantStandingForADatabase(), EnlistmentOptions.None);
                            taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat");
                         }))
                        .Must().Throw<TransactionAbortedException>().Which;

         [PCT] public void the_unit_of_work_rolls_back_because_dotnet_refuses_a_distributed_transaction() => _exception.InnerException.Must().BeAssignableTo<NotSupportedException>();

         [PCT] public void the_reader_gets_the_meaning_committed_before() => Reader.Get<StoredVocab>(VocabId).Meaning.Must().Be("");

         [PCT] public void the_file_still_holds_the_meaning_committed_before() => File.ReadAllText(VocabFileByDefault).Must().Be(SerializedVocab(VocabId, "taberu", ""));

         [PCT] public void the_stores_own_folder_holds_no_temporary_file() => FilesInTheStoresOwnFolder.Must().BeEmpty();
      }

      ///<summary>Another program puts a file where the store's own folder is, after the unit of work's write has prepared and before its<br/>
      /// commit, so the commit can't write its journal: the commit hasn't happened, and the unit of work can still roll back. Only on Unix,<br/>
      /// as above: on Windows nothing is set up, and every specification is skipped.</summary>
      public class and_a_unit_of_work_changing_it_fails_to_write_its_journal : after_a_unit_of_work_adds_a_vocab
      {
         readonly Exception? _exception;

         public and_a_unit_of_work_changing_it_fails_to_write_its_journal()
         {
            if(!OnUnix) return;
            _exception = Invoking(() => InUnitOfWork(taggregates =>
                         {
                            taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat");
                            Transaction.Current!.EnlistVolatile(new ParticipantThatPutsAFileWhereAFolderIs(StoresOwnFolder), EnlistmentOptions.None);
                         }))
                        .Must().Throw<TransactionAbortedException>().Which;
         }

         [PCT(Skip = OnlyUnixLetsAProgramReplaceTheFolderOfAHeldLock, SkipType = typeof(Given_a_memory_image_state_store_kept_in_files), SkipUnless = nameof(OnUnix))]
         public void the_unit_of_work_rolls_back_with_the_disks_exception() => _exception!.InnerException.Must().BeAssignableTo<IOException>();

         [PCT(Skip = OnlyUnixLetsAProgramReplaceTheFolderOfAHeldLock, SkipType = typeof(Given_a_memory_image_state_store_kept_in_files), SkipUnless = nameof(OnUnix))]
         public void the_reader_gets_the_meaning_committed_before() => Reader.Get<StoredVocab>(VocabId).Meaning.Must().Be("");

         [PCT(Skip = OnlyUnixLetsAProgramReplaceTheFolderOfAHeldLock, SkipType = typeof(Given_a_memory_image_state_store_kept_in_files), SkipUnless = nameof(OnUnix))]
         public void the_file_still_holds_the_meaning_committed_before() => File.ReadAllText(VocabFileByDefault).Must().Be(SerializedVocab(VocabId, "taberu", ""));

         [PCT(Skip = OnlyUnixLetsAProgramReplaceTheFolderOfAHeldLock, SkipType = typeof(Given_a_memory_image_state_store_kept_in_files), SkipUnless = nameof(OnUnix))]
         public void once_the_file_is_gone_a_unit_of_work_changing_it_commits()
         {
            File.Delete(StoresOwnFolder);
            InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to drink"));
            File.ReadAllText(VocabFileByDefault).Must().Be(SerializedVocab(VocabId, "taberu", "to drink"));
         }
      }
   }

   public class whose_folder_a_service_of_the_container_gives : Given_a_memory_image_state_store_kept_in_files
   {
      protected whose_folder_a_service_of_the_container_gives() => _folderGivenByAService = true;

      public class after_a_unit_of_work_adds_a_vocab_there : whose_folder_a_service_of_the_container_gives
      {
         public after_a_unit_of_work_adds_a_vocab_there() => AddTheVocab("taberu");

         [PCT] public void the_vocab_is_in_its_file_under_the_folder_the_service_gives() =>
            VocabFiles.Must().SequenceEqual([Path.GetRelativePath(VocabFolder.FullName, VocabFileByDefault)]);
      }
   }

   ///<summary>A file stands where the store's own folder goes, as it can on a consumer's disk, before any store has started on the folder.</summary>
   public class when_a_file_stands_where_the_stores_own_folder_goes : Given_a_memory_image_state_store_kept_in_files
   {
      readonly IOException _exception;

      public when_a_file_stands_where_the_stores_own_folder_goes()
      {
         _folder.Create();
         File.WriteAllText(StoresOwnFolder, "");
         _exception = Invoking(() => Reader.TryGet(VocabId, out StoredVocab? _)).Must().Throw<IOException>().Which;
      }

      [PCT] public void the_first_use_of_a_store_started_on_the_folder_fails_with_the_disks_exception_naming_the_own_folder() => _exception.Message.Must().Contain(StoresOwnFolder);
   }

   public class when_a_crash_left_a_journal_and_the_temporary_file_it_renames : Given_a_memory_image_state_store_kept_in_files
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

   public class when_a_crash_left_temporary_files_and_no_journal : Given_a_memory_image_state_store_kept_in_files
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

   public class when_a_vocabs_file_isnt_json : Given_a_memory_image_state_store_kept_in_files
   {
      public when_a_vocabs_file_isnt_json()
      {
         Directory.CreateDirectory(Path.GetDirectoryName(VocabFileByDefault)!);
         File.WriteAllText(VocabFileByDefault, "not json");
      }

      [PCT] public void the_store_fails_at_its_first_use_naming_the_file() =>
         Invoking(() => Reader.TryGet(VocabId, out StoredVocab? _)).Must().Throw<InvalidOperationException>().Which.Message.Must().Contain(VocabFileByDefault);
   }

   public class when_two_files_hold_one_vocab : Given_a_memory_image_state_store_kept_in_files
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

   public class when_composed : Given_a_memory_image_state_store_kept_in_files
   {
      static void Compose(Action<MemoryImageStateStoreBuilder> declare) => TestEnv.DIContainer.CreateTestingContainerBuilder().Registrar.MemoryImageStateStore(declare);

      [PCT] public void without_the_files_of_a_class_it_keeps_fails_naming_the_class() =>
         Invoking(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                              .Taggregate(() => new StoredSentence())
                                              .StoredInJsonFiles(_folder, Options, files => files.FolderOf<StoredVocab>("vocab"))))
           .Must().Throw<InvalidOperationException>().Which.Message.Must().Contain(nameof(StoredSentence));

      [PCT] public void with_two_classes_in_one_folder_fails() =>
         Invoking(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                              .Taggregate(() => new StoredSentence())
                                              .StoredInJsonFiles(_folder, Options, files => files.FolderOf<StoredVocab>("notes").FolderOf<StoredSentence>("notes"))))
           .Must().Throw<InvalidOperationException>();

      [PCT] public void with_a_classs_folder_inside_another_classs_fails() =>
         Invoking(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                              .Taggregate(() => new StoredSentence())
                                              .StoredInJsonFiles(_folder, Options, files => files.FolderOf<StoredVocab>("notes").FolderOf<StoredSentence>("notes/sentences"))))
           .Must().Throw<InvalidOperationException>();

      [PCT] public void with_the_stores_own_folder_as_a_classs_folder_fails() =>
         Invoking(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                              .StoredInJsonFiles(_folder, Options, files => files.FolderOf<StoredVocab>(".state-store"))))
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

   ///<summary>A durable participant of a transaction, as a SQL Server connection is: it commits in a single phase when it is the<br/>
   /// transaction's one durable participant, and votes to commit otherwise.</summary>
   sealed class DurableParticipantStandingForADatabase : ISinglePhaseNotification
   {
      public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment) => singlePhaseEnlistment.Committed();
      public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();
      public void Commit(Enlistment enlistment) => enlistment.Done();
      public void Rollback(Enlistment enlistment) => enlistment.Done();
      public void InDoubt(Enlistment enlistment) => enlistment.Done();
   }

   ///<summary>Another program, which, when the transaction prepares, replaces the folder <paramref name="folder"/> with a file of the same name.</summary>
   sealed class ParticipantThatPutsAFileWhereAFolderIs(string folder) : IEnlistmentNotification
   {
      readonly string _folder = folder;

      public void Prepare(PreparingEnlistment preparingEnlistment)
      {
         Directory.Delete(_folder, recursive: true);
         File.WriteAllText(_folder, "");
         preparingEnlistment.Prepared();
      }

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
