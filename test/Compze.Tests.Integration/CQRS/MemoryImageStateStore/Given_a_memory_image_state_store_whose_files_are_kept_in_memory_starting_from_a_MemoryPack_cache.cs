using System.Text.Json;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.MemoryImageStateStore.MemoryPack;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image state store keeping vocabs in JSON files kept in memory, starting from a MemoryPack cache that is kept in memory too, under<br/>
/// a folder that doesn't exist on the disk and must never come to.</summary>
public class Given_a_state_store_whose_files_are_kept_in_memory_starting_from_a_MemoryPack_cache : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-files-in-memory-cached", Guid.NewGuid().ToString("N")));
   readonly IDependencyInjectionContainer _store;

   protected Given_a_state_store_whose_files_are_kept_in_memory_starting_from_a_MemoryPack_cache()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new CachedVocab())
                                                 .StoredInJsonFiles(_folder, Options, files => files.StartingFromAMemoryPackCache()
                                                                                                .KeptInMemory()
                                                                                                .FolderOf<CachedVocab>("vocab")));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _store = builder.Build();
   }

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   string VocabFile => Path.Combine(_folder.FullName, "vocab", VocabId.Value.ToString("N")[..2], $"{VocabId.Value}.json");

   string CacheFile => Path.Combine(_folder.FullName, ".state-store", "vocab.load-cache");

   ITaggregateFilesInMemory Files => _store.RootResolver.Resolve<ITaggregateFilesInMemory>();

   ITaggregateStoreReader Reader => _store.RootResolver.Resolve<ITaggregateStoreReader>();

   void Reload() => _store.RootResolver.Resolve<IMemoryImageStateStore>().LoadFromStorage();

   protected override async Task DisposeAsyncInternal() => await _store.DisposeAsync();

   public class after_a_unit_of_work_adds_a_vocab_and_the_store_reloads_reading_its_file : Given_a_state_store_whose_files_are_kept_in_memory_starting_from_a_MemoryPack_cache
   {
      public after_a_unit_of_work_adds_a_vocab_and_the_store_reloads_reading_its_file()
      {
         _store.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<ITaggregateStoreUpdater>().Add(CachedVocab.Create(VocabId, "taberu", "to eat")));
         Reload();
      }

      [PCT] public void the_files_are_the_vocabs_file_and_the_cache_in_the_stores_own_folder() => Files.Paths.Order(StringComparer.Ordinal).Must().SequenceEqual([CacheFile, VocabFile]);

      [PCT] public void nothing_is_written_to_the_disk() => Directory.Exists(_folder.FullName).Must().BeFalse();

      public class and_the_store_reloads_with_nothing_changed : after_a_unit_of_work_adds_a_vocab_and_the_store_reloads_reading_its_file
      {
         public and_the_store_reloads_with_nothing_changed() => Reload();

         [PCT] public void the_store_has_the_vocab() => Reader.Get<CachedVocab>(VocabId).Meaning.Must().Be("to eat");
      }

      ///<summary>Written behind the store at once, so possibly within the same tick of the system's clock as the store's own write.</summary>
      public class and_its_file_is_changed_behind_the_store_to_text_of_the_same_length_and_the_store_reloads : after_a_unit_of_work_adds_a_vocab_and_the_store_reloads_reading_its_file
      {
         public and_its_file_is_changed_behind_the_store_to_text_of_the_same_length_and_the_store_reloads()
         {
            Files.WriteAllText(VocabFile, Files.ReadAllText(VocabFile).Replace("\"to eat\"", "\"to EAT\"", StringComparison.Ordinal));
            Reload();
         }

         [PCT] public void the_store_has_the_meaning_written_behind_it_not_the_cached_one() => Reader.Get<CachedVocab>(VocabId).Meaning.Must().Be("to EAT");
      }
   }
}
