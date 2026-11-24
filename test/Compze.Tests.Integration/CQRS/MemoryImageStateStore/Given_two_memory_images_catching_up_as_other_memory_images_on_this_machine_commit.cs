using System.Text.Json;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>Two memory images of a store keeping vocabs in the tables of a domain database of the current test's persistence layer, each<br/>
/// declared to catch up as other memory images on this machine commit, each a container of its own on the database. Both have read a<br/>
/// vocab the first added, and what the first's <see cref="IMemoryImageStateStore.ChangesCommittedElsewhere"/> tells is collected.</summary>
public class Given_two_memory_images_catching_up_as_other_memory_images_on_this_machine_commit : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { Converters = { new TaggregateIdAsAGuid() } };
   static readonly WaitTimeout LongerThanCatchingUpCouldTake = WaitTimeout.Seconds(10);
   readonly IDependencyInjectionContainer _theFirst;
   readonly IDependencyInjectionContainer _theSecond;
   readonly IAwaitableThreadShared<List<ChangesFoundInStorage>> _toldTheFirst = IAwaitableThreadShared.New(new List<ChangesFoundInStorage>());

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   public Given_two_memory_images_catching_up_as_other_memory_images_on_this_machine_commit()
   {
      _theFirst = BuildTheFirstMemoryImage();
      _theSecond = _theFirst.CloneAndBuild();
      _theFirst.RootResolver.Resolve<IMemoryImageStateStore>().ChangesCommittedElsewhere.Subscribe(changes => _toldTheFirst.Update(it => it.Add(changes)));
      InUnitOfWorkOf(_theFirst, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "taberu")));
      ReaderOf(_theSecond).Get<StoredVocab>(VocabId);
   }

   static IDependencyInjectionContainer BuildTheFirstMemoryImage()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.CurrentTestsDomainDatabaseWithTheMemoryImageStateStoresSqlLayer("memory-images-catching-up-as-others-commit");
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                           .StoredInTheDomainDatabase(Options, rows => rows.CatchingUpAsOtherMemoryImagesOnThisMachineCommit()
                                                                                                           .ClassNamed<StoredVocab>("vocab")));
      builder.Registrar.LocalTessagingEngine(_ => {});
      return builder.Build();
   }

   static void InUnitOfWorkOf(IDependencyInjectionContainer memoryImage, Action<ITaggregateStoreUpdater> work) =>
      memoryImage.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   static ITaggregateStoreReader ReaderOf(IDependencyInjectionContainer memoryImage) => memoryImage.RootResolver.Resolve<ITaggregateStoreReader>();

   ///<summary>The ids of every taggregate the first has been told changed elsewhere, once it has been told of <paramref name="id"/>,<br/>
   /// or what it has been told when it hasn't within the time catching up could take.</summary>
   IReadOnlyList<TaggregateId> IdsTheFirstIsToldChangedOnceToldOf(TaggregateId id)
   {
      _toldTheFirst.TryAwait(it => it.Any(changes => changes.Changed.Contains(id)), timeout: LongerThanCatchingUpCouldTake);
      return _toldTheFirst.Read(it => it.SelectMany(changes => changes.Changed).ToList());
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _theSecond.DisposeAsync();
      await _theFirst.DisposeAsync();
   }

   public class after_a_unit_of_work_of_the_second_changes_the_vocabs_meaning : Given_two_memory_images_catching_up_as_other_memory_images_on_this_machine_commit
   {
      readonly IReadOnlyList<TaggregateId> _toldChanged;

      public after_a_unit_of_work_of_the_second_changes_the_vocabs_meaning()
      {
         InUnitOfWorkOf(_theSecond, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat"));
         _toldChanged = IdsTheFirstIsToldChangedOnceToldOf(VocabId);
      }

      [PCT] public void the_first_is_told_with_no_unit_of_work_of_its_own_that_the_vocab_changed() => _toldChanged.Must().SequenceEqual([VocabId]);

      [PCT] public void the_first_reads_the_new_meaning_outside_any_unit_of_work() => ReaderOf(_theFirst).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
   }
}
