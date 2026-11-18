using System.Text.Json;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.StateStore;
using Compze.Teventive.StateStore.MemoryPack;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>A state store keeping vocabs in JSON files, in a fresh folder of its own, starting from a MemoryPack cache. Each store<br/>
/// started is a container of its own on the same folder, as each start of an application is.</summary>
public class Given_a_state_store_kept_in_files_starting_from_a_MemoryPack_cache : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
   readonly List<IDependencyInjectionContainer> _containers = [];
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-memorypack-cache", Guid.NewGuid().ToString("N")));
   bool _keepsVocabsWithTags;
   IDependencyInjectionContainer? _store;

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   string VocabFile => Path.Combine(_folder.FullName, "vocab", VocabId.Value.ToString("N")[..2], $"{VocabId.Value}.json");

   string StoresOwnFolder => Path.Combine(_folder.FullName, ".state-store");

   IReadOnlyList<string> FilesInTheStoresOwnFolder => Directory.Exists(StoresOwnFolder) ? [..new DirectoryInfo(StoresOwnFolder).EnumerateFiles().Select(it => it.Name)] : [];

   ///<summary>The store last started.</summary>
   IDependencyInjectionContainer Store => _store!;

   ITaggregateStoreReader Reader => Store.RootResolver.Resolve<ITaggregateStoreReader>();

   IReadOnlyList<Guid> IdsOfAllVocabs => [..Store.RootResolver.Resolve<ITaggregateStoreBulkReader>().All<CachedVocab>().Select(it => it.Id.Value).Order()];

   ///<summary>Starts a store on the folder, and has it load the files, as its first use does.</summary>
   void StartAStore()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.StateStore(store =>
      {
         if(_keepsVocabsWithTags)
            store.Taggregate(() => new CachedVocabWithTags())
                 .StoredInFiles(_folder, Options, files => files.Of<CachedVocabWithTags>("vocab").CachedWithMemoryPack());
         else
            store.Taggregate(() => new CachedVocab())
                 .StoredInFiles(_folder, Options, files => files.Of<CachedVocab>("vocab").CachedWithMemoryPack());
      });
      builder.Registrar.LocalTessagingEngine(_ => {});
      _store = builder.Build();
      _containers.Add(_store);
      LoadTheFiles();

      void LoadTheFiles()
      {
         var bulkReader = Store.RootResolver.Resolve<ITaggregateStoreBulkReader>();
         _ = _keepsVocabsWithTags ? bulkReader.All<CachedVocabWithTags>().Count : bulkReader.All<CachedVocab>().Count;
      }
   }

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      Store.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ///<summary>Changes the vocab's meaning in its file behind the store, as another program could, to text of the same length.</summary>
   void ChangeTheMeaningInTheVocabsFile(bool keepingItsLastWriteTime)
   {
      var lastWriteTime = File.GetLastWriteTimeUtc(VocabFile);
      File.WriteAllText(VocabFile, File.ReadAllText(VocabFile).Replace("\"to eat\"", "\"to EAT\"", StringComparison.Ordinal));
      File.SetLastWriteTimeUtc(VocabFile, keepingItsLastWriteTime ? lastWriteTime : lastWriteTime.AddMinutes(1));
   }

   protected override async Task DisposeAsyncInternal()
   {
      foreach(var container in _containers) await container.DisposeAsync();
      if(_folder.Exists) _folder.Delete(recursive: true);
   }

   public class after_a_store_adds_a_vocab_and_a_second_store_starts_on_the_folder : Given_a_state_store_kept_in_files_starting_from_a_MemoryPack_cache
   {
      public after_a_store_adds_a_vocab_and_a_second_store_starts_on_the_folder()
      {
         StartAStore();
         InUnitOfWork(taggregates => taggregates.Add(CachedVocab.Create(VocabId, "taberu", "to eat")));
         StartAStore();
      }

      [PCT] public void the_second_store_has_the_vocab() => Reader.Get<CachedVocab>(VocabId).Meaning.Must().Be("to eat");

      [PCT] public void the_stores_own_folder_holds_one_cache_file_and_no_temporary_file() =>
         FilesInTheStoresOwnFolder.Must().Satisfy(it => it.Count == 1 && !it[0].EndsWith(".being-written", StringComparison.Ordinal));

      public class and_the_vocabs_file_changes_behind_the_stores_keeping_its_length_and_last_write_time : after_a_store_adds_a_vocab_and_a_second_store_starts_on_the_folder
      {
         public and_the_vocabs_file_changes_behind_the_stores_keeping_its_length_and_last_write_time()
         {
            ChangeTheMeaningInTheVocabsFile(keepingItsLastWriteTime: true);
            StartAStore();
         }

         [PCT] public void a_third_store_has_the_meaning_the_cache_holds_since_it_didnt_read_the_file() => Reader.Get<CachedVocab>(VocabId).Meaning.Must().Be("to eat");
      }

      public class and_the_vocabs_file_changes_behind_the_stores_with_a_new_last_write_time : after_a_store_adds_a_vocab_and_a_second_store_starts_on_the_folder
      {
         public and_the_vocabs_file_changes_behind_the_stores_with_a_new_last_write_time()
         {
            ChangeTheMeaningInTheVocabsFile(keepingItsLastWriteTime: false);
            StartAStore();
         }

         [PCT] public void a_third_store_reads_the_changed_file() => Reader.Get<CachedVocab>(VocabId).Meaning.Must().Be("to EAT");
      }

      public class and_the_second_store_adds_another_vocab : after_a_store_adds_a_vocab_and_a_second_store_starts_on_the_folder
      {
         readonly TaggregateId _otherVocabId = new(Guid.NewGuid());

         public and_the_second_store_adds_another_vocab()
         {
            InUnitOfWork(taggregates => taggregates.Add(CachedVocab.Create(_otherVocabId, "nomu", "to drink")));
            StartAStore();
         }

         [PCT] public void a_third_store_has_both_vocabs() => IdsOfAllVocabs.Must().SequenceEqual(new[] { VocabId.Value, _otherVocabId.Value }.Order());
      }

      public class and_the_second_store_deletes_the_vocab : after_a_store_adds_a_vocab_and_a_second_store_starts_on_the_folder
      {
         public and_the_second_store_deletes_the_vocab()
         {
            InUnitOfWork(taggregates => taggregates.GetForUpdate<CachedVocab>(VocabId).Delete());
            StartAStore();
         }

         [PCT] public void a_third_store_has_no_vocab() => IdsOfAllVocabs.Must().BeEmpty();
      }

      public class and_the_vocabs_file_changes_behind_the_stores_keeping_its_length_and_last_write_time_and_a_store_keeping_vocabs_with_tags_starts
         : after_a_store_adds_a_vocab_and_a_second_store_starts_on_the_folder
      {
         public and_the_vocabs_file_changes_behind_the_stores_keeping_its_length_and_last_write_time_and_a_store_keeping_vocabs_with_tags_starts()
         {
            ChangeTheMeaningInTheVocabsFile(keepingItsLastWriteTime: true);
            _keepsVocabsWithTags = true;
            StartAStore();
         }

         [PCT] public void it_reads_the_file_since_the_cache_holds_snapshots_of_another_shape() => Reader.Get<CachedVocabWithTags>(VocabId).Meaning.Must().Be("to EAT");

         [PCT] public void the_stores_own_folder_still_holds_one_cache_file() => FilesInTheStoresOwnFolder.Must().HaveCount(1);
      }
   }

   public class composed_to_keep_a_class_whose_snapshot_type_MemoryPack_doesnt_serialize : Given_a_state_store_kept_in_files_starting_from_a_MemoryPack_cache
   {
      [PCT] public void fails_naming_the_snapshot_type() =>
         Invoking(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                               .Registrar.StateStore(store => store.Taggregate(() => new StoredVocab())
                                                                   .StoredInFiles(_folder, Options, files => files.Of<StoredVocab>("vocab").CachedWithMemoryPack())))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Contain(nameof(StoredVocabSnapshot));
   }
}
