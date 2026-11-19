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
using Compze.Teventive.MemoryImageStateStore.Testing;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image state store keeping vocabs and sentences in JSON files on a file system in memory, under a folder that doesn't exist on the disk and<br/>
/// must never come to: vocabs in "vocab", sentences in "sentences", each in the default layout.</summary>
public class Given_a_memory_image_state_store_whose_files_are_on_a_file_system_in_memory : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new TaggregateIdAsAGuid() } };
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-files-in-memory", Guid.NewGuid().ToString("N")));
   readonly IDependencyInjectionContainer _store;

   protected Given_a_memory_image_state_store_whose_files_are_on_a_file_system_in_memory()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                 .Taggregate(() => new StoredSentence())
                                                 .StoredInJsonFiles(_folder, Options, files => files.OnAFileSystemInMemory()
                                                                                                .FolderOf<StoredVocab>("vocab")
                                                                                                .FolderOf<StoredSentence>("sentences")));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _store = builder.Build();
   }

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   ///<summary>The vocab's file in the default layout: &lt;first two hex digits of the id&gt;/&lt;id&gt;.json under the vocab folder.</summary>
   string VocabFile => Path.Combine(_folder.FullName, "vocab", VocabId.Value.ToString("N")[..2], $"{VocabId.Value}.json");

   IFileSystemInMemory Files => _store.RootResolver.Resolve<IFileSystemInMemory>();

   ITaggregateStoreReader Reader => _store.RootResolver.Resolve<ITaggregateStoreReader>();

   ChangesFoundInStorage Reload() => _store.RootResolver.Resolve<IMemoryImageStateStore>().LoadFromStorage();

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      _store.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   static string SerializedVocab(TaggregateId id, string question, string meaning) => JsonSerializer.Serialize(new StoredVocabSnapshot(id, question, meaning), Options);

   protected override async Task DisposeAsyncInternal() => await _store.DisposeAsync();

   public class after_a_unit_of_work_adds_a_vocab : Given_a_memory_image_state_store_whose_files_are_on_a_file_system_in_memory
   {
      public after_a_unit_of_work_adds_a_vocab() => InUnitOfWork(taggregates => taggregates.Add(StoredVocab.Create(VocabId, "taberu")));

      [PCT] public void the_files_are_the_vocabs_file_alone_in_the_default_layout() => Files.Paths.Must().SequenceEqual([VocabFile]);

      [PCT] public void the_vocabs_file_holds_exactly_what_the_consumers_options_make_of_its_snapshot() => Files.ReadAllText(VocabFile).Must().Be(SerializedVocab(VocabId, "taberu", ""));

      [PCT] public void nothing_is_written_to_the_disk() => Directory.Exists(_folder.FullName).Must().BeFalse();

      public class and_the_store_reloads : after_a_unit_of_work_adds_a_vocab
      {
         readonly ChangesFoundInStorage _reloaded;

         public and_the_store_reloads() => _reloaded = Reload();

         [PCT] public void the_reload_reports_nothing_removed() => _reloaded.Removed.Must().BeEmpty();

         [PCT] public void the_reload_reports_nothing_changed() => _reloaded.Changed.Must().BeEmpty();

         [PCT] public void the_store_has_the_vocab() => Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }

      public class and_its_file_is_changed_behind_the_store_and_the_store_reloads : after_a_unit_of_work_adds_a_vocab
      {
         readonly ChangesFoundInStorage _reloaded;

         public and_its_file_is_changed_behind_the_store_and_the_store_reloads()
         {
            Files.WriteAllText(VocabFile, SerializedVocab(VocabId, "taberu", "to eat"));
            _reloaded = Reload();
         }

         [PCT] public void the_reload_reports_the_vocab_changed() => _reloaded.Changed.Must().SequenceEqual([VocabId]);

         [PCT] public void the_store_has_the_meaning_written_behind_it() => Reader.Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
      }

      public class and_its_file_is_deleted_behind_the_store_and_the_store_reloads : after_a_unit_of_work_adds_a_vocab
      {
         readonly ChangesFoundInStorage _reloaded;

         public and_its_file_is_deleted_behind_the_store_and_the_store_reloads()
         {
            Files.Delete(VocabFile);
            _reloaded = Reload();
         }

         [PCT] public void the_reload_reports_the_vocab_removed() => _reloaded.Removed.Must().SequenceEqual([VocabId]);
      }

      public class and_a_unit_of_work_deletes_it : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_deletes_it() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());

         [PCT] public void there_are_no_files() => Files.Paths.Must().BeEmpty();
      }
   }

   public class whose_files_hold_a_vocab_written_before_the_stores_first_use_which_then_gets_it : Given_a_memory_image_state_store_whose_files_are_on_a_file_system_in_memory
   {
      readonly StoredVocab _gotten;

      public whose_files_hold_a_vocab_written_before_the_stores_first_use_which_then_gets_it()
      {
         Files.WriteAllText(VocabFile, SerializedVocab(VocabId, "nomu", "to drink"));
         _gotten = Reader.Get<StoredVocab>(VocabId);
      }

      [PCT] public void the_store_loaded_it() => _gotten.Meaning.Must().Be("to drink");

      [PCT] public void nothing_is_written_to_the_disk() => Directory.Exists(_folder.FullName).Must().BeFalse();
   }
}
