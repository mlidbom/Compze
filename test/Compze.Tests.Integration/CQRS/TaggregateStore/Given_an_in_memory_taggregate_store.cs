using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
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
using Compze.Teventive.TaggregateStore;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.TaggregateStore;

///<summary>A prototype: an in-memory taggregate store, with a LocalTessagingEngine dispatching what its taggregates publish.</summary>
public class Given_an_in_memory_taggregate_store : UniversalTestBase
{
   IDependencyInjectionContainer? _container;
   protected IDependencyInjectionContainer Container => _container!;
   protected TaggregateId VocabId { get; } = new(Guid.NewGuid());
   protected ConcurrentQueue<string> ParticipationLog { get; } = new();
   protected ConcurrentQueue<string> ObservationLog { get; } = new();

   protected void Compose(Action<TessageBusHandlerRegistrar>? participation = null, Action<TeventObservationRegistrar>? observation = null)
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.InMemoryTaggregateStore();
      builder.Registrar.LocalTessagingEngine(engine =>
      {
         if(participation != null) engine.RegisterTessageBusHandlers(participation);
         if(observation != null) engine.ObserveTevents(observation);
      });
      _container = builder.Build();
   }

   protected void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   protected ITaggregateStoreReader Reader => Container.RootResolver.Resolve<ITaggregateStoreReader>();

   private protected StoredVocab CommittedVocab(TaggregateId id) => Reader.Get<StoredVocab>(id);

   protected void WaitForTheObservers() => Container.RootResolver.Resolve<ILocalTessagingEngine>().StopAsync().GetAwaiter().GetResult();

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null) await _container.DisposeAsync();
   }

   public class after_a_unit_of_work_saves_a_new_vocab : Given_an_in_memory_taggregate_store
   {
      bool _saveHasReturned;

      public after_a_unit_of_work_saves_a_new_vocab()
      {
         Compose(participation: handle => handle.ForTevent((IStoredVocabCreated it) =>
                 {
                    ParticipationLog.Enqueue($"created {it.Question}, save had returned: {_saveHasReturned}");
                    return Task.CompletedTask;
                 }),
                 observation: observe => observe.ForTevent((IStoredVocabCreated it) => ObservationLog.Enqueue($"created {it.Question}")));

         InUnitOfWork(taggregates =>
         {
            taggregates.Save(StoredVocab.Create(VocabId, "taberu"));
            _saveHasReturned = true;
         });
         WaitForTheObservers();
      }

      [PCT] public void the_reader_gets_the_vocab() => CommittedVocab(VocabId).Question.Must().Be("taberu");
      [PCT] public void the_participation_handler_ran_once_and_only_after_save_had_returned() => ParticipationLog.Must().SequenceEqual(["created taberu, save had returned: True"]);
      [PCT] public void the_observer_observed_the_creation() => ObservationLog.Must().SequenceEqual(["created taberu"]);
      [PCT] public void publishing_on_the_instance_the_reader_gets_throws() => Invoking(() => CommittedVocab(VocabId).ChangeMeaning("to eat")).Must().Throw<Exception>();
   }

   public class after_a_unit_of_work_changes_the_meaning_of_a_saved_vocab_twice : Given_an_in_memory_taggregate_store
   {
      bool _bothDomainCallsHaveReturned;
      readonly StoredVocab _instanceCommittedBefore;
      string? _meaningTheReaderGotDuringTheUnitOfWork;
      string? _meaningTheReaderGotWhenTheUnitOfWorkHadCommitted;

      public after_a_unit_of_work_changes_the_meaning_of_a_saved_vocab_twice()
      {
         Compose(participation: handle => handle.ForTevent((IStoredVocabMeaningChanged it, ITaggregateStoreUpdater taggregates) =>
         {
            ParticipationLog.Enqueue($"{it.Meaning}; both domain calls had returned: {_bothDomainCallsHaveReturned}; the copy's meaning: {taggregates.Get<StoredVocab>(VocabId).Meaning}");
            return Task.CompletedTask;
         }));
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));
         _instanceCommittedBefore = CommittedVocab(VocabId);

         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
         {
            unitOfWork.OnCommittedSuccessfully(() => _meaningTheReaderGotWhenTheUnitOfWorkHadCommitted = CommittedVocab(VocabId).Meaning);
            var vocab = unitOfWork.Resolve<ITaggregateStoreUpdater>().Get<StoredVocab>(VocabId);
            vocab.ChangeMeaning("to eat");
            _meaningTheReaderGotDuringTheUnitOfWork = CommittedVocab(VocabId).Meaning;
            vocab.ChangeMeaning("to eat, to consume");
            _bothDomainCallsHaveReturned = true;
         });
      }

      [PCT] public void the_participation_handler_ran_for_each_change_in_order_after_both_domain_calls_had_returned() =>
         ParticipationLog.Must().SequenceEqual(["to eat; both domain calls had returned: True; the copy's meaning: to eat, to consume",
                                        "to eat, to consume; both domain calls had returned: True; the copy's meaning: to eat, to consume"]);

      [PCT] public void during_the_unit_of_work_the_reader_got_the_committed_meaning() => _meaningTheReaderGotDuringTheUnitOfWork.Must().Be("");
      [PCT] public void when_the_unit_of_work_had_committed_the_reader_got_the_new_meaning() => _meaningTheReaderGotWhenTheUnitOfWorkHadCommitted.Must().Be("to eat, to consume");
      [PCT] public void the_reader_now_gets_the_new_meaning() => CommittedVocab(VocabId).Meaning.Must().Be("to eat, to consume");
      [PCT] public void the_reader_now_gets_another_instance() => ReferenceEquals(CommittedVocab(VocabId), _instanceCommittedBefore).Must().BeFalse();
      [PCT] public void the_instance_committed_before_still_has_the_meaning_it_had() => _instanceCommittedBefore.Meaning.Must().Be("");
   }

   public class after_a_unit_of_work_that_changed_a_vocab_throws_before_it_completes : Given_an_in_memory_taggregate_store
   {
      public after_a_unit_of_work_that_changed_a_vocab_throws_before_it_completes()
      {
         Compose(participation: handle => handle.ForTevent((IStoredVocabMeaningChanged it) =>
                 {
                    ParticipationLog.Enqueue(it.Meaning);
                    return Task.CompletedTask;
                 }),
                 observation: observe => observe.ForTevent((IStoredVocabMeaningChanged it) => ObservationLog.Enqueue(it.Meaning)));
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));

         Invoking(() => InUnitOfWork(taggregates =>
                 {
                    taggregates.Get<StoredVocab>(VocabId).ChangeMeaning("to eat");
                    throw new InvalidOperationException("The unit of work fails.");
                 }))
           .Must().Throw<InvalidOperationException>();
         WaitForTheObservers();
      }

      [PCT] public void the_reader_gets_the_meaning_committed_before() => CommittedVocab(VocabId).Meaning.Must().Be("");
      [PCT] public void no_participation_handler_ran() => ParticipationLog.Must().BeEmpty();
      [PCT] public void no_observer_observed_the_change() => ObservationLog.Must().BeEmpty();
   }

   public class after_a_participation_handler_changes_another_vocab : Given_an_in_memory_taggregate_store
   {
      readonly TaggregateId _followingVocabId = new(Guid.NewGuid());

      public after_a_participation_handler_changes_another_vocab()
      {
         Compose(participation: handle => handle.ForTevent((IStoredVocabMeaningChanged it, ITaggregateStoreUpdater taggregates) =>
                 {
                    if(it.TaggregateId == VocabId) taggregates.Get<StoredVocab>(_followingVocabId).ChangeMeaning($"follows: {it.Meaning}");
                    return Task.CompletedTask;
                 }),
                 observation: observe => observe.ForTevent((IStoredVocabMeaningChanged it) => ObservationLog.Enqueue(it.Meaning)));
         InUnitOfWork(taggregates =>
         {
            taggregates.Save(StoredVocab.Create(VocabId, "taberu"));
            taggregates.Save(StoredVocab.Create(_followingVocabId, "kuu"));
         });

         InUnitOfWork(taggregates => taggregates.Get<StoredVocab>(VocabId).ChangeMeaning("to eat"));
         WaitForTheObservers();
      }

      [PCT] public void the_other_vocab_is_changed() => CommittedVocab(_followingVocabId).Meaning.Must().Be("follows: to eat");
      [PCT] public void the_observer_observed_the_cause_before_the_consequence() => ObservationLog.Must().SequenceEqual(["to eat", "follows: to eat"]);
   }

   public class after_a_participation_handler_changes_the_vocab_whose_tevent_it_handles : Given_an_in_memory_taggregate_store
   {
      public after_a_participation_handler_changes_the_vocab_whose_tevent_it_handles()
      {
         Compose(participation: handle => handle
                                         .ForTevent((IStoredVocabMeaningChanged it, ITaggregateStoreUpdater taggregates) =>
                                          {
                                             if(it.Meaning != it.Meaning.Trim()) taggregates.Get<StoredVocab>(VocabId).ChangeMeaning(it.Meaning.Trim());
                                             return Task.CompletedTask;
                                          })
                                         .ForTevent((IStoredVocabMeaningChanged it) =>
                                          {
                                             ParticipationLog.Enqueue(it.Meaning);
                                             return Task.CompletedTask;
                                          }),
                 observation: observe => observe.ForTevent((IStoredVocabMeaningChanged it) => ObservationLog.Enqueue(it.Meaning)));
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));

         InUnitOfWork(taggregates => taggregates.Get<StoredVocab>(VocabId).ChangeMeaning("  to eat  "));
         WaitForTheObservers();
      }

      [PCT] public void the_vocab_has_the_handlers_meaning() => CommittedVocab(VocabId).Meaning.Must().Be("to eat");
      [PCT] public void a_later_participation_handler_got_both_tevents_in_the_order_published() => ParticipationLog.Must().SequenceEqual(["  to eat  ", "to eat"]);
      [PCT] public void the_observer_observed_both_tevents_in_the_order_published() => ObservationLog.Must().SequenceEqual(["  to eat  ", "to eat"]);
   }

   public class after_code_catches_the_exception_of_a_failing_publish_and_completes_the_unit_of_work : Given_an_in_memory_taggregate_store
   {
      readonly Exception _exception;

      public after_code_catches_the_exception_of_a_failing_publish_and_completes_the_unit_of_work()
      {
         Compose();
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));

         _exception = Invoking(() => InUnitOfWork(taggregates =>
                                {
                                   var vocab = taggregates.Get<StoredVocab>(VocabId);
                                   vocab.ChangeMeaning("to eat");
                                   try
                                   {
                                      vocab.ChangeMeaning(StoredVocab.MeaningThatFailsToApply);
                                   }
                                   catch(InvalidOperationException) {}
                                }))
                     .Must().Throw<TransactionAbortedException>().Which;
      }

      [PCT] public void the_unit_of_work_rolls_back_saying_a_publish_failed() => _exception.InnerException!.Message.Must().Contain("A publish failed");
      [PCT] public void the_reader_gets_the_meaning_committed_before() => CommittedVocab(VocabId).Meaning.Must().Be("");
   }

   public class when_two_units_of_work_on_two_threads_change_one_vocab : Given_an_in_memory_taggregate_store
   {
      readonly ManualResetEventSlim _theFirstUnitOfWorkHasTheVocab = new();
      readonly string _meaningTheSecondUnitOfWorkGot;

      public when_two_units_of_work_on_two_threads_change_one_vocab()
      {
         Compose();
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));

         var first = Task.Run(() => InUnitOfWork(taggregates =>
         {
            var vocab = taggregates.Get<StoredVocab>(VocabId);
            _theFirstUnitOfWorkHasTheVocab.Set();
            Thread.Sleep(200); //Gives the second unit of work time to reach Get while the first holds the vocab.
            vocab.ChangeMeaning("first");
         }));
         _theFirstUnitOfWorkHasTheVocab.Wait();

         var meaningTheSecondGot = "";
         InUnitOfWork(taggregates =>
         {
            var vocab = taggregates.Get<StoredVocab>(VocabId);
            meaningTheSecondGot = vocab.Meaning;
            vocab.ChangeMeaning($"{vocab.Meaning}, second");
         });
         _meaningTheSecondUnitOfWorkGot = meaningTheSecondGot;
         first.Wait();
      }

      [PCT] public void the_second_waited_for_the_first_to_commit() => _meaningTheSecondUnitOfWorkGot.Must().Be("first");
      [PCT] public void both_changes_are_committed_in_order() => CommittedVocab(VocabId).Meaning.Must().Be("first, second");
   }

   public class when_measuring : Given_an_in_memory_taggregate_store
   {
      const int Iterations = 20_000;

      [PCT] public void what_an_edit_costs()
      {
         var reportFile = Environment.GetEnvironmentVariable("TAGGREGATE_STORE_MEASUREMENT_FILE");
         if(reportFile == null) return;

         Compose(participation: handle => handle.ForTevent((IStoredVocabMeaningChanged _) => Task.CompletedTask));
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));
         var committed = CommittedVocab(VocabId);

         var report = new List<string>
         {
            Measure("an empty unit of work", _ => Container.ScopeFactory.ExecuteUnitOfWork(_ => {})),
            Measure("a unit of work that gets one vocab and changes it", iteration => InUnitOfWork(taggregates => taggregates.Get<StoredVocab>(VocabId).ChangeMeaning(iteration.ToString(CultureInfo.InvariantCulture)))),
            Measure("copying a vocab through its snapshot", _ => committed.TakeSnapshot().Restore(VocabId, committed.Version)),
            Measure("a new vocab, created by its creation tevent", _ => StoredVocab.Create(new TaggregateId(Guid.NewGuid()), "taberu"))
         };
         File.AppendAllLines(reportFile, report);
      }

      static string Measure(string what, Action<int> operation)
      {
         for(var iteration = 0; iteration < Iterations / 10; iteration++) operation(iteration);
         var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
         var stopwatch = Stopwatch.StartNew();
         for(var iteration = 0; iteration < Iterations; iteration++) operation(iteration);
         stopwatch.Stop();
         var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
         return $"{what}: {stopwatch.Elapsed.TotalMicroseconds / Iterations:F2} µs and {allocated / Iterations:N0} B allocated, each, over {Iterations:N0}";
      }
   }
}
