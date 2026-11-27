using System.Collections.Concurrent;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Underscore;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.TessageBus;

///<summary>An exactly-once endpoint whose handler of <see cref="DeferredStep"/> is deferred: it never runs in its sender's unit of<br/>
/// work, since the endpoint's inbox runs it in one of its own once the sender has committed. The handler sends the step's<br/>
/// next step to the endpoint itself while the step says there are steps after it. The endpoint keeps its identity and its<br/>
/// pooled database across host rebuilds, so a specification can stop it and start it again.</summary>
public abstract class Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler : UniversalTestBase
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);

   readonly IDependencyInjectionContainer _rootContainer;
   readonly ConcurrentQueue<StepTry> _tries = new();
   readonly ConcurrentQueue<HandledStep> _handled = new();
   readonly ConcurrentQueue<int> _stepsWhoseSendersCommitted = new();
   readonly ConcurrentQueue<int> _stepsWhoseTriesWereCancelled = new();
   readonly ConcurrentQueue<UnitOfWorkId> _sendingUnitsOfWork = new();
   TestingEndpointHost? _host;
   ExactlyOnceEndpoint _endpoint = null!;
   ExactlyOnceEndpoint? _otherEndpoint;

   protected Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler() =>
      _rootContainer = TestEnv.DIContainer.CreateTestingContainerBuilder()
                              ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer())
                              .Build();

   ///<summary>Passed through once by each try of a step's handling, failed or not.</summary>
   protected IThreadGate TriesGate { get; } = IThreadGate.NewOpen(HandlingTimeout, nameof(TriesGate));

   ///<summary>Passed through once by each step handled.</summary>
   protected IThreadGate HandledGate { get; } = IThreadGate.NewOpen(HandlingTimeout, nameof(HandledGate));

   ///<summary>Whether the first try of step 1's handling fails.</summary>
   protected bool TheFirstStepsFirstTryFails { get; set; }

   ///<summary>Whether the first try of step 1's handling waits for the token its handler was handed to be cancelled, and gives<br/>
   /// up then; it fails if the token isn't cancelled within a minute.</summary>
   protected bool TheFirstStepsFirstTryWaitsForItsCancellation { get; set; }

   ///<summary>Whether the host runs another exactly-once endpoint, with no handlers, beside the one with the deferred handler.</summary>
   protected bool AnotherEndpointRuns { get; set; }

   protected IReadOnlyList<int> HandledStepNumbers => [.. _handled.Select(it => it.Number)];
   protected HandledStep HandlingOf(int stepNumber) => _handled.Single(it => it.Number == stepNumber);
   protected IReadOnlyList<HandledStep> Handled => [.. _handled];
   protected IReadOnlyList<StepTry> TriesOf(int stepNumber) => [.. _tries.Where(it => it.StepNumber == stepNumber)];
   protected IReadOnlyList<int> NumbersOfTheStepsWhoseTriesWereCancelled => [.. _stepsWhoseTriesWereCancelled];

   ///<summary>The units of work that sent steps from outside the endpoint's handlers, in the order they sent them.</summary>
   protected IReadOnlyList<UnitOfWorkId> SendingUnitsOfWork => [.. _sendingUnitsOfWork];

   ///<summary>Builds a host of the endpoint over its database and starts it.</summary>
   protected async Task StartHostAsync()
   {
      BuildHost();
      await StartTheHostAsync();
   }

   ///<summary>Builds a host of the endpoint over its database, without starting it.</summary>
   protected void BuildHost()
   {
      _host = TestingEndpointHost.Create(_rootContainer);
      _endpoint = _host.RegisterEndpoint(new TheEndpoint(this));
      if(AnotherEndpointRuns) _otherEndpoint = _host.RegisterEndpoint(new TheOtherEndpoint());
   }

   ///<summary>Starts the host <see cref="BuildHost"/> built, and awaits its endpoints meeting.</summary>
   protected async Task StartTheHostAsync()
   {
      await _host!.StartAsync();
      if(AnotherEndpointRuns) await _host.AwaitEndpointsHaveMetEachOtherAsync();
   }

   protected override async Task InitializeAsyncInternal() => await StartHostAsync();

   ///<summary>Sends <paramref name="stepNumbers"/>' steps, in order, in one unit of work, which then commits.</summary>
   protected async Task SendInOneUnitOfWorkAsync(params int[] stepNumbers) =>
      await InAUnitOfWorkOfTheEndpointAsync(async unitOfWork =>
      {
         _sendingUnitsOfWork.Enqueue(unitOfWork.Id);
         foreach(var stepNumber in stepNumbers)
            await SendAsync(new DeferredStep(stepNumber), unitOfWork, CancellationToken.None);
      });

   ///<summary>Sends a step with <paramref name="stepsAfterIt"/> steps after it, in a unit of work that then commits.</summary>
   protected async Task SendAFlowAsync(int stepsAfterIt) =>
      await InAUnitOfWorkOfTheEndpointAsync(async unitOfWork =>
      {
         _sendingUnitsOfWork.Enqueue(unitOfWork.Id);
         await SendAsync(new DeferredStep(1, stepsAfterIt), unitOfWork, CancellationToken.None);
      });

   ///<summary>Sends step <paramref name="stepNumber"/> from the other endpoint (<see cref="AnotherEndpointRuns"/>).</summary>
   protected async Task SendFromTheOtherEndpointAsync(int stepNumber) =>
      await _otherEndpoint!.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new DeferredStep(stepNumber));

   ///<summary>Sends step <paramref name="stepNumber"/> in a unit of work that then throws, so that it rolls back.</summary>
   protected async Task SendInAUnitOfWorkThatRollsBackAsync(int stepNumber)
   {
      try
      {
         await InAUnitOfWorkOfTheEndpointAsync(async unitOfWork =>
         {
            await SendAsync(new DeferredStep(stepNumber), unitOfWork, CancellationToken.None);
            throw new UnitOfWorkRolledBackOnPurposeException();
         });
      }
      catch(UnitOfWorkRolledBackOnPurposeException) {}
   }

   ///<summary>Stops the endpoint, its stop's drain ended by <paramref name="cancellationToken"/>: see <see cref="IEndpoint.StopAsync"/>.</summary>
   protected async Task StopTheEndpointAsync(CancellationToken cancellationToken) => await _endpoint.StopAsync(cancellationToken);

   ///<summary>Disposes the host without waiting for its endpoint to come to rest, for a specification that stops the endpoint<br/>
   /// while a step is still being tried.</summary>
   protected async Task DisposeHostWithoutWaitingForItsEndpointToBeAtRestAsync()
   {
      var host = _host!;
      _host = null;
      await host.DisposeWithoutWaitingForEndpointsToBeAtRestAsync();
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_host != null) await _host.DisposeAsync();
      await _rootContainer.DisposeAsync();
   }

   async Task InAUnitOfWorkOfTheEndpointAsync(Func<IUnitOfWorkResolver, Task> work) =>
      await _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWorkAsync(work);

   //Registered before the send, so it runs before the hook with which the send makes the step ready to handle at the commit.
   async Task SendAsync(DeferredStep step, IUnitOfWorkResolver unitOfWork, CancellationToken cancellationToken)
   {
      unitOfWork.OnCommittedSuccessfully(() => _stepsWhoseSendersCommitted.Enqueue(step.Number));
      await unitOfWork.Resolve<IUnitOfWorkTommandSender>().SendAsync(step, cancellationToken);
   }

   async Task HandleAsync(DeferredStep step, IUnitOfWorkResolver unitOfWork, CancellationToken cancellationToken)
   {
      //The endpoint handles one step at a time, so no other try runs while this one counts.
      var tryNumber = _tries.Count(it => it.StepNumber == step.Number) + 1;
      var stepTry = new StepTry(step.Number, tryNumber);
      _tries.Enqueue(stepTry);
      await TriesGate.AwaitPassThroughAsync();

      if(step.Number == 1 && tryNumber == 1)
      {
         if(TheFirstStepsFirstTryWaitsForItsCancellation) await WaitForTheCancellationOfAsync(stepTry, cancellationToken);
         if(TheFirstStepsFirstTryFails) throw new IntentionalStepFailureException(step.Number);
      }

      var itsSenderHadCommitted = _stepsWhoseSendersCommitted.Contains(step.Number);
      if(step.StepsAfterIt > 0) await SendAsync(new DeferredStep(step.Number + 1, step.StepsAfterIt - 1), unitOfWork, cancellationToken);

      _handled.Enqueue(new HandledStep(step.Number, unitOfWork.Id, itsSenderHadCommitted));
      await HandledGate.AwaitPassThroughAsync();
   }

   async Task WaitForTheCancellationOfAsync(StepTry stepTry, CancellationToken cancellationToken)
   {
      try
      {
         await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
      }
      catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
      {
         _stepsWhoseTriesWereCancelled.Enqueue(stepTry.StepNumber);
         throw;
      }
      throw new TimeoutException($"The token handed to try {stepTry.TryNumber} of step {stepTry.StepNumber} was not cancelled within a minute.");
   }

   public class when_a_unit_of_work_sends_it_a_step_and_commits : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await SendInOneUnitOfWorkAsync(1);
         await HandledGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
      }

      [PCT] public void the_handler_handles_the_step() => HandledStepNumbers.Must().SequenceEqual([1]);

      [PCT] public void the_handler_runs_in_a_unit_of_work_other_than_the_senders() => HandlingOf(1).UnitOfWork.Must().NotBe(SendingUnitsOfWork.Single());

      [PCT] public void the_handler_runs_once_the_sender_has_committed() => HandlingOf(1).ItsSenderHadCommitted.Must().BeTrue();
   }

   public class when_a_unit_of_work_sends_it_a_step_and_rolls_back_and_another_then_sends_it_a_step_and_commits : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await SendInAUnitOfWorkThatRollsBackAsync(1);
         await SendInOneUnitOfWorkAsync(2);
         await HandledGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
      }

      //Step 1 would be handled before step 2, which its sender sent after it, had it been admitted.
      [PCT] public void the_handler_handles_only_the_step_whose_sender_committed() => HandledStepNumbers.Must().SequenceEqual([2]);
   }

   public class when_five_units_of_work_send_it_a_step_each_one_after_another : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         foreach(var stepNumber in Enumerable.Range(1, 5))
            await SendInOneUnitOfWorkAsync(stepNumber);
         await HandledGate.AwaitPassedThroughCountEqualToAsync(5, HandlingTimeout);
      }

      [PCT] public void the_handler_handles_the_steps_in_the_order_their_senders_committed() => HandledStepNumbers.Must().SequenceEqual([1, 2, 3, 4, 5]);
   }

   public class when_one_unit_of_work_sends_it_three_steps : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await SendInOneUnitOfWorkAsync(1, 2, 3);
         await HandledGate.AwaitPassedThroughCountEqualToAsync(3, HandlingTimeout);
      }

      [PCT] public void the_handler_handles_the_steps_in_the_order_they_were_sent() => HandledStepNumbers.Must().SequenceEqual([1, 2, 3]);

      [PCT] public void the_handler_handles_each_step_in_a_unit_of_work_of_its_own() =>
         Handled.Select(it => it.UnitOfWork).Must().Satisfy(unitsOfWork => unitsOfWork.Distinct().Count() == 3);
   }

   public class when_a_unit_of_work_sends_it_a_step_whose_handler_sends_the_next_until_the_third : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await SendAFlowAsync(stepsAfterIt: 2);
         await HandledGate.AwaitPassedThroughCountEqualToAsync(3, HandlingTimeout);
      }

      [PCT] public void the_handler_handles_the_three_steps_in_order() => HandledStepNumbers.Must().SequenceEqual([1, 2, 3]);

      [PCT] public void the_handler_handles_each_step_in_a_unit_of_work_of_its_own() =>
         Handled.Select(it => it.UnitOfWork).Must().Satisfy(unitsOfWork => unitsOfWork.Distinct().Count() == 3);

      [PCT] public void the_handler_handles_each_step_once_the_handling_of_the_step_before_it_has_committed() =>
         Handled.Must().Satisfy(steps => steps.All(it => it.ItsSenderHadCommitted));
   }

   public class when_another_endpoint_sends_it_a_step : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      protected override async Task InitializeAsyncInternal()
      {
         AnotherEndpointRuns = true;
         await base.InitializeAsyncInternal();
         await SendFromTheOtherEndpointAsync(1);
         await HandledGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
      }

      [PCT] public void the_handler_handles_the_step() => HandledStepNumbers.Must().SequenceEqual([1]);
   }

   public class when_a_unit_of_work_sends_it_a_step_before_it_has_started : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      string _whyTheSendWasRefused = "";

      protected override async Task InitializeAsyncInternal()
      {
         BuildHost();
         _whyTheSendWasRefused = (await InvokingAsync(async () => await SendInOneUnitOfWorkAsync(1)).Must().ThrowAsync<InvalidOperationException>()).Which.Message;
         await StartTheHostAsync();
      }

      [PCT] public void the_send_is_refused_since_the_endpoint_has_not_yet_started() => _whyTheSendWasRefused.Must().Contain("before it had started");
   }

   public class when_the_first_try_of_a_steps_handling_fails : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      protected override async Task InitializeAsyncInternal()
      {
         TheFirstStepsFirstTryFails = true;
         await base.InitializeAsyncInternal();
         await SendInOneUnitOfWorkAsync(1);
         await HandledGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
      }

      //The send above returned without throwing: the sender never sees the handler fail.
      [PCT] public void the_handler_tries_the_step_twice() => TriesOf(1).Count.Must().Be(2);

      [PCT] public void the_handler_handles_the_step() => HandledStepNumbers.Must().SequenceEqual([1]);
   }

   public class when_the_endpoint_stops_with_a_cancelled_token_while_a_steps_handler_runs_and_starts_again : Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler
   {
      protected override async Task InitializeAsyncInternal()
      {
         TheFirstStepsFirstTryWaitsForItsCancellation = true;
         await base.InitializeAsyncInternal();
         await SendInOneUnitOfWorkAsync(1);
         await TriesGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);

         await StopTheEndpointAsync(new CancellationToken(canceled: true));
         await DisposeHostWithoutWaitingForItsEndpointToBeAtRestAsync();

         await StartHostAsync();
         await HandledGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
      }

      [PCT] public void the_running_try_is_cancelled() => NumbersOfTheStepsWhoseTriesWereCancelled.Must().SequenceEqual([1]);

      [PCT] public void the_endpoints_next_process_handles_the_step() => HandledStepNumbers.Must().SequenceEqual([1]);
   }

   ///<summary>One try of a step's handling, as its handler began it.</summary>
   protected class StepTry(int stepNumber, int tryNumber)
   {
      public int StepNumber { get; } = stepNumber;
      public int TryNumber { get; } = tryNumber;
   }

   ///<summary>A step's handler completing: in which unit of work, and whether the unit of work that sent the step had committed<br/>
   /// when the handler began.</summary>
   protected class HandledStep(int number, UnitOfWorkId unitOfWork, bool itsSenderHadCommitted)
   {
      public int Number { get; } = number;
      public UnitOfWorkId UnitOfWork { get; } = unitOfWork;
      public bool ItsSenderHadCommitted { get; } = itsSenderHadCommitted;
   }

   class UnitOfWorkRolledBackOnPurposeException() : Exception("The specification rolls this unit of work back on purpose.");

   class IntentionalStepFailureException(int stepNumber) : Exception($"Intentional failure of the first try of step {stepNumber}");

   class TheOtherEndpoint : ExactlyOnceEndpointDeclaration<TheOtherEndpoint>, IEndpointIdentity
   {
      public static string Name => "DeferredStepSender";
      public static EndpointId Id => new(Guid.Parse("2D7B4E90-6A13-4C58-9F21-B3E05C8A7D14"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();
   }

   class TheEndpoint : ExactlyOnceEndpointDeclaration<TheEndpoint>, IEndpointIdentity
   {
      public static string Name => "DeferredHandlers";
      public static EndpointId Id => new(Guid.Parse("9E3C5A71-2B84-4D6F-A0C9-58E17B23D4F6"));

      readonly Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler _specification;
      internal TheEndpoint(Given_an_exactly_once_endpoint_with_a_deferred_tommand_handler specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();

      protected override void RegisterDeferredTommandHandlers(IDeferredTommandHandlerRegistrar handle) => handle
         .ForTommand<DeferredStep>(_specification.HandleAsync);
   }
}
