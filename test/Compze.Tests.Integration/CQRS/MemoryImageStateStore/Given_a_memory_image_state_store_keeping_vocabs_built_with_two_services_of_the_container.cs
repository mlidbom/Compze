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
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image state store keeping vocabs in files kept in memory, its vocab class declared with a registration of the container whose<br/>
/// factory takes two singleton services: the store builds every vocab it restores through the container.</summary>
public class Given_a_state_store_keeping_vocabs_built_with_two_services_of_the_container : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { Converters = { new TaggregateIdAsAGuid() } };
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-with-dependencies", Guid.NewGuid().ToString("N")));
   readonly IDependencyInjectionContainer _container;

   protected Given_a_state_store_keeping_vocabs_built_with_two_services_of_the_container()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.Register(Singleton.For<VocabDictionary>().CreatedBy(() => new VocabDictionary()),
                                 Singleton.For<VocabReadings>().CreatedBy(() => new VocabReadings()));
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(TrackedTransient.For<StoredVocab>().CreatedBy((VocabDictionary dictionary, VocabReadings readings) => new StoredVocab(dictionary, readings)))
                                                 .StoredInJsonFiles(_folder, Options, files => files.KeptInMemory().FolderOf<StoredVocab>("vocab")));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _container = builder.Build();
      InUnitOfWork(taggregates => taggregates.Add(StoredVocab.Create(VocabId, "taberu")));
   }

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   bool WasBuiltWithTheContainersServices(StoredVocab vocab) =>
      ReferenceEquals(vocab.Dictionary, _container.RootResolver.Resolve<VocabDictionary>())
   && ReferenceEquals(vocab.Readings, _container.RootResolver.Resolve<VocabReadings>());

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class when_a_unit_of_work_changes_the_vocab : Given_a_state_store_keeping_vocabs_built_with_two_services_of_the_container
   {
      readonly StoredVocab _theCopyTheUnitOfWorkChanged;

      public when_a_unit_of_work_changes_the_vocab()
      {
         StoredVocab? copy = null;
         InUnitOfWork(taggregates =>
         {
            copy = taggregates.GetForUpdate<StoredVocab>(VocabId);
            copy.ChangeMeaning("to eat");
         });
         _theCopyTheUnitOfWorkChanged = copy!;
      }

      [PCT] public void the_copy_the_unit_of_work_changed_was_built_with_the_containers_two_services() =>
         WasBuiltWithTheContainersServices(_theCopyTheUnitOfWorkChanged).Must().BeTrue();

      [PCT] public void the_reader_gets_the_change() => Reader.Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
   }

   public class when_the_store_reloads_its_files : Given_a_state_store_keeping_vocabs_built_with_two_services_of_the_container
   {
      public when_the_store_reloads_its_files() => _container.RootResolver.Resolve<IMemoryImageStateStore>().LoadFromStorage();

      [PCT] public void the_vocab_the_store_restored_was_built_with_the_containers_two_services() =>
         WasBuiltWithTheContainersServices(Reader.Get<StoredVocab>(VocabId)).Must().BeTrue();

      [PCT] public void the_vocab_holds_its_state() => Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
   }
}
