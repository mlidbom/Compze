using System.Diagnostics;
using System.Text.Json;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Diagnostics;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image of a store keeping vocabs in the tables of a domain database of the current test's persistence layer, which has added a<br/>
/// vocab, and so has loaded, and holds the tables, when a second memory image starts on the database: each a container of its own, as each<br/>
/// process is. The store is declared to wait for another memory image to release the storage as long as each nested class says.</summary>
public abstract class Given_a_memory_image_of_a_store_kept_in_the_domain_database_holding_its_tables : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { Converters = { new TaggregateIdAsAGuid() } };

   ///<summary>The container each memory image is a clone of: clones share its database of the test database pool. It never uses the store itself.</summary>
   readonly IDependencyInjectionContainer _composition;

   bool _theFirstIsDisposed;

   protected TaggregateId VocabId { get; } = new(Guid.NewGuid());

   protected IDependencyInjectionContainer TheFirst { get; }

   protected IDependencyInjectionContainer TheSecond { get; }

   protected Given_a_memory_image_of_a_store_kept_in_the_domain_database_holding_its_tables(WaitTimeout waitingForAnotherMemoryImageToReleaseTheStorage)
   {
      _composition = BuildTheComposition(waitingForAnotherMemoryImageToReleaseTheStorage);
      TheFirst = _composition.CloneAndBuild();
      InUnitOfWorkOf(TheFirst, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "taberu")));
      TheSecond = _composition.CloneAndBuild();
   }

   static IDependencyInjectionContainer BuildTheComposition(WaitTimeout waitingForAnotherMemoryImageToReleaseTheStorage)
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.CurrentTestsDomainDatabaseWithTheMemoryImageStateStoresSqlLayer("memory-images-holding-the-tables");
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                           .WaitingForAnotherMemoryImageToReleaseTheStorage(waitingForAnotherMemoryImageToReleaseTheStorage)
                                                           .StoredInTheDomainDatabase(Options, rows => rows.ClassNamed<StoredVocab>("vocab")));
      builder.Registrar.LocalTessagingEngine(_ => {});
      return builder.Build();
   }

   protected static void InUnitOfWorkOf(IDependencyInjectionContainer memoryImage, Action<ITaggregateStoreUpdater> work) =>
      memoryImage.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   protected static ITaggregateStoreReader ReaderOf(IDependencyInjectionContainer memoryImage) => memoryImage.RootResolver.Resolve<ITaggregateStoreReader>();

   ///<summary>Disposes the first memory image, as its process does as it ends, which releases the tables.</summary>
   protected void DisposeTheFirst()
   {
      TheFirst.Dispose();
      _theFirstIsDisposed = true;
   }

   protected static string TheHolderAsItRecordsItself => $"held for the memory image state store kept in the domain database's tables, by process {Environment.ProcessId} (";

   protected override async Task DisposeAsyncInternal()
   {
      await TheSecond.DisposeAsync();
      if(!_theFirstIsDisposed) await TheFirst.DisposeAsync();
      await _composition.DisposeAsync();
   }

   public class and_the_second_is_declared_to_wait_for_nothing : Given_a_memory_image_of_a_store_kept_in_the_domain_database_holding_its_tables
   {
      readonly TimeoutException _refusal;

      public and_the_second_is_declared_to_wait_for_nothing() : base(WaitTimeout.Zero) =>
         _refusal = Invoking(() => ReaderOf(TheSecond).TryGet<StoredVocab>(VocabId, out _)).Must().Throw<TimeoutException>().Which;

      [PCT] public void the_seconds_first_use_fails_saying_another_memory_image_uses_the_tables() =>
         _refusal.Message.Must().Contain("The memory image state store kept in the domain database's tables is used by another memory image");

      [PCT] public void the_refusal_names_the_process_holding_the_tables_its_machine_and_since_when() =>
         _refusal.Message.Must().Contain(TheHolderAsItRecordsItself).Contain($") on {Environment.MachineName}, since ");

      [PCT] public void the_refusal_says_how_to_have_a_memory_image_wait_for_the_holder() =>
         _refusal.Message.Must().Contain(nameof(MemoryImageStateStoreBuilder.WaitingForAnotherMemoryImageToReleaseTheStorage));

      public class and_a_unit_of_work_of_the_first_then_changes_the_vocabs_meaning : and_the_second_is_declared_to_wait_for_nothing
      {
         public and_a_unit_of_work_of_the_first_then_changes_the_vocabs_meaning() =>
            InUnitOfWorkOf(TheFirst, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat"));

         [PCT] public void the_first_reads_the_new_meaning() => ReaderOf(TheFirst).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
      }

      public class and_the_first_is_then_disposed : and_the_second_is_declared_to_wait_for_nothing
      {
         public and_the_first_is_then_disposed() => DisposeTheFirst();

         [PCT] public void the_seconds_next_use_loads_the_vocab_the_first_added() => ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }
   }

   public class and_the_second_is_declared_to_wait_half_a_second : Given_a_memory_image_of_a_store_kept_in_the_domain_database_holding_its_tables
   {
      readonly TimeoutException _refusal;
      readonly TimeSpan _waited;

      public and_the_second_is_declared_to_wait_half_a_second() : base(WaitTimeout.Milliseconds(500))
      {
         var waiting = Stopwatch.StartNew();
         _refusal = Invoking(() => ReaderOf(TheSecond).TryGet<StoredVocab>(VocabId, out _)).Must().Throw<TimeoutException>().Which;
         _waited = waiting.Elapsed;
      }

      [PCT] public void the_seconds_first_use_fails_once_the_half_second_has_passed() => _waited.Must().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(500));

      [PCT] public void the_refusal_says_how_long_it_waited_and_names_the_process_holding_the_tables() =>
         _refusal.Message.Must().Contain($"which this one waited {TimeSpan.FromMilliseconds(500)} for to release it").Contain(TheHolderAsItRecordsItself);
   }

   public class and_the_second_is_declared_to_wait_ten_seconds_and_the_first_is_disposed_while_it_waits : Given_a_memory_image_of_a_store_kept_in_the_domain_database_holding_its_tables
   {
      string? _questionTheSecondRead;

      public and_the_second_is_declared_to_wait_ten_seconds_and_the_first_is_disposed_while_it_waits() : base(WaitTimeout.Seconds(10)) {}

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         var theSecondsFirstUse = TaskCE.RunOnDedicatedThreadAsync(() => _questionTheSecondRead = ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Question);
         await Task.Delay(TimeSpan.FromMilliseconds(300)); //The second has begun waiting by then, far more often than not; if not, it takes the free tables later.
         DisposeTheFirst();
         await theSecondsFirstUse.WaitAsyncDumpingTheProcessOnTimeout(TimeSpan.FromSeconds(10), "the second memory image's first use, waiting for the first to release the tables");
      }

      [PCT] public void the_second_loads_the_vocab_the_first_added() => _questionTheSecondRead.Must().Be("taberu");
   }
}
