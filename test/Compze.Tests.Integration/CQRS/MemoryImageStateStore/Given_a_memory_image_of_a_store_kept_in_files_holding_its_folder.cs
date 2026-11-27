using System.Diagnostics;
using System.Text.Json;
using System.Transactions;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.Hosting.Testing.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
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

///<summary>A memory image of a store keeping vocabs in JSON files, which has added a vocab, and so has loaded, and holds the store's folder, when a<br/>
/// second memory image starts on the same folder: each a container of its own, as each process is. The store is declared to wait for another<br/>
/// memory image to release the storage as long as each nested class says.</summary>
public abstract class Given_a_memory_image_of_a_store_kept_in_files_holding_its_folder : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new();
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-held", Guid.NewGuid().ToString("N")));
   bool _theFirstIsDisposed;

   protected TaggregateId VocabId { get; } = new(Guid.NewGuid());

   protected IDependencyInjectionContainer TheFirst { get; }

   protected IDependencyInjectionContainer TheSecond { get; }

   protected Given_a_memory_image_of_a_store_kept_in_files_holding_its_folder(WaitTimeout waitingForAnotherMemoryImageToReleaseTheStorage)
   {
      TheFirst = StartAMemoryImage(waitingForAnotherMemoryImageToReleaseTheStorage);
      InUnitOfWorkOf(TheFirst, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "taberu")));
      TheSecond = StartAMemoryImage(waitingForAnotherMemoryImageToReleaseTheStorage);
   }

   IDependencyInjectionContainer StartAMemoryImage(WaitTimeout waitingForAnotherMemoryImageToReleaseTheStorage)
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                           .WaitingForAnotherMemoryImageToReleaseTheStorage(waitingForAnotherMemoryImageToReleaseTheStorage)
                                                           .StoredInJsonFiles(_folder, Options, files => files.FolderOf<StoredVocab>("vocab")));
      builder.Registrar.LocalTessagingEngine(_ => {});
      return builder.Build();
   }

   protected static void InUnitOfWorkOf(IDependencyInjectionContainer memoryImage, Action<ITaggregateStoreUpdater> work) =>
      memoryImage.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   protected static ITaggregateStoreReader ReaderOf(IDependencyInjectionContainer memoryImage) => memoryImage.RootResolver.Resolve<ITaggregateStoreReader>();

   protected string Folder => _folder.FullName;

   ///<summary>Disposes the first memory image, as its process does as it ends, which releases the folder.</summary>
   protected void DisposeTheFirst()
   {
      TheFirst.Dispose();
      _theFirstIsDisposed = true;
   }

   protected string TheHolderAsItRecordsItself => $"held for the memory image state store kept in {Folder}, by process {Environment.ProcessId} (";

   protected override async Task DisposeAsyncInternal()
   {
      await TheSecond.DisposeAsync();
      if(!_theFirstIsDisposed) await TheFirst.DisposeAsync();
      if(_folder.Exists) _folder.Delete(recursive: true);
   }

   public class and_the_second_is_declared_to_wait_for_nothing : Given_a_memory_image_of_a_store_kept_in_files_holding_its_folder
   {
      readonly TimeoutException _refusal;

      public and_the_second_is_declared_to_wait_for_nothing() : base(WaitTimeout.Zero) =>
         _refusal = Invoking(() => ReaderOf(TheSecond).TryGet<StoredVocab>(VocabId, out _)).Must().Throw<TimeoutException>().Which;

      [PCT] public void the_seconds_first_use_fails_saying_another_memory_image_uses_the_folder() =>
         _refusal.Message.Must().Contain($"The memory image state store kept in {Folder} is used by another memory image");

      [PCT] public void the_refusal_names_the_process_holding_the_folder_its_machine_and_since_when() =>
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

      ///<summary>A unit of work of the first changes the vocab's meaning, and the first is disposed before the unit of work commits, as a host<br/>
      /// may dispose its container while work still runs. The second then holds the folder, and a unit of work of it changes the meaning too,<br/>
      /// before the first's unit of work commits.</summary>
      public class and_a_unit_of_work_of_the_first_changing_the_vocab_commits_after_the_first_is_disposed_and_the_second_has_changed_it : and_the_second_is_declared_to_wait_for_nothing
      {
         readonly TransactionAbortedException _theFirstsCommitFailure;
         readonly ChangesFoundInStorage _aReloadOfTheSecond;

         public and_a_unit_of_work_of_the_first_changing_the_vocab_commits_after_the_first_is_disposed_and_the_second_has_changed_it()
         {
            _theFirstsCommitFailure = Invoking(() => InUnitOfWorkOf(TheFirst, taggregates =>
                                      {
                                         taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat");
                                         DisposeTheFirst();
                                         TransactionScopeCe.SuppressAmbient(() => InUnitOfWorkOf(TheSecond, theSeconds => theSeconds.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to drink")));
                                      }))
                                     .Must().Throw<TransactionAbortedException>().Which;
            _aReloadOfTheSecond = TheSecond.RootResolver.Resolve<IMemoryImageStateStore>().LoadFromStorage();
         }

         [PCT] public void the_first_units_of_work_rolls_back_because_its_store_is_disposed() =>
            _theFirstsCommitFailure.InnerException!.Message.Must().Contain($"The memory image state store kept in {Folder} is disposed");

         [PCT] public void a_reload_of_the_second_finds_no_taggregate_changed() => _aReloadOfTheSecond.Changed.Must().BeEmpty();

         [PCT] public void after_a_reload_the_second_reads_the_meaning_it_gave() => ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Meaning.Must().Be("to drink");
      }

      ///<summary>The first is disposed while a unit of work of it, changing the vocab's meaning, commits: the store's write is under way, prepared<br/>
      /// and not yet committed, as the disposal begins.</summary>
      public class and_the_first_is_disposed_while_a_unit_of_work_of_it_changing_the_vocab_commits : and_the_second_is_declared_to_wait_for_nothing
      {
         bool _theDisposalEndedBeforeTheCommit;

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            var disposingTheFirst = new ParticipantThatBeginsDisposingAMemoryImageWhileItsWriteIsUnderWay(DisposeTheFirst);
            InUnitOfWorkOf(TheFirst, taggregates =>
            {
               taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat");
               Transaction.Current!.EnlistVolatile(disposingTheFirst, EnlistmentOptions.None);
            });
            await disposingTheFirst.Disposal.WaitAsyncDumpingTheProcessOnTimeout(TimeSpan.FromSeconds(10), "the disposal of the first memory image, begun while its unit of work committed");
            _theDisposalEndedBeforeTheCommit = disposingTheFirst.TheDisposalEndedBeforeTheCommit;
         }

         [PCT] public void the_disposal_ends_only_once_the_unit_of_work_has_committed() => _theDisposalEndedBeforeTheCommit.Must().BeFalse();

         [PCT] public void the_second_loads_the_meaning_the_unit_of_work_gave() => ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
      }
   }

   public class and_the_second_is_declared_to_wait_half_a_second : Given_a_memory_image_of_a_store_kept_in_files_holding_its_folder
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

      [PCT] public void the_refusal_says_how_long_it_waited_and_names_the_process_holding_the_folder() =>
         _refusal.Message.Must().Contain($"which this one waited {TimeSpan.FromMilliseconds(500)} for to release it").Contain(TheHolderAsItRecordsItself);
   }

   public class and_the_second_is_declared_to_wait_ten_seconds_and_the_first_is_disposed_while_it_waits : Given_a_memory_image_of_a_store_kept_in_files_holding_its_folder
   {
      string? _questionTheSecondRead;

      public and_the_second_is_declared_to_wait_ten_seconds_and_the_first_is_disposed_while_it_waits() : base(WaitTimeout.Seconds(10)) {}

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         var theSecondsFirstUse = TaskCE.RunOnDedicatedThreadAsync(() => _questionTheSecondRead = ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Question);
         await Task.Delay(TimeSpan.FromMilliseconds(300)); //The second has begun waiting by then, far more often than not; if not, it takes the free folder later.
         DisposeTheFirst();
         await theSecondsFirstUse.WaitAsyncDumpingTheProcessOnTimeout(TimeSpan.FromSeconds(10), "the second memory image's first use, waiting for the first to release the folder");
      }

      [PCT] public void the_second_loads_the_vocab_the_first_added() => _questionTheSecondRead.Must().Be("taberu");
   }
}
