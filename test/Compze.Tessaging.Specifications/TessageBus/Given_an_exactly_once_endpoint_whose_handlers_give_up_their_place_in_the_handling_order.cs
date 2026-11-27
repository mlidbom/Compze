using System.Collections.Concurrent;
using System.Diagnostics;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.TessageBus;

///<summary>An exactly-once endpoint whose handlers choose their <see cref="HandlingOrder"/>, and another exactly-once endpoint<br/>
/// that sends and publishes to it. Each tessage is numbered in the order the specification sends or publishes it. The tries of<br/>
/// number 1's handling fail, or its handler waits, for as long as the specification says, until another tessage has been handled.</summary>
public abstract class Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order : UniversalTestBase
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);

   readonly TestingEndpointHost _host = TestingEndpointHost.Create();
   readonly ExactlyOnceEndpoint _endpoint;
   readonly ExactlyOnceEndpoint _otherEndpoint;
   readonly ConcurrentQueue<int> _handled = new();
   int _triesOfTheFirst;
   long _theFirstsFirstTryStartedAt;

   protected Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order()
   {
      _endpoint = _host.RegisterEndpoint(new TheEndpoint(this));
      _otherEndpoint = _host.RegisterEndpoint(new TheOtherEndpoint());
   }

   ///<summary>Passed through once by each tessage handled.</summary>
   IThreadGate HandledGate { get; } = IThreadGate.NewOpen(HandlingTimeout, nameof(HandledGate));

   ///<summary>Passed through by each try of number 1's handling that fails.</summary>
   IThreadGate TheFirstsFailedTriesGate { get; } = IThreadGate.NewOpen(HandlingTimeout, nameof(TheFirstsFailedTriesGate));

   ///<summary>Passed through by number 1's handler as it begins to wait for another tessage to be handled.</summary>
   IThreadGate TheFirstsHandlerWaitsGate { get; } = IThreadGate.NewOpen(HandlingTimeout, nameof(TheFirstsHandlerWaitsGate));

   ///<summary>For how long, from its first try, the tries of number 1's handling fail while no other tessage has been handled:<br/>
   /// not at all by default.</summary>
   protected TimeSpan HowLongTheFirstsTriesFailWhileNoOtherIsHandled { get; set; } = TimeSpan.Zero;

   ///<summary>How long number 1's handler waits for another tessage to be handled before it ends: not at all by default.</summary>
   protected WaitTimeout HowLongTheFirstsHandlerWaitsForAnotherToBeHandled { get; set; } = WaitTimeout.Zero;

   ///<summary>The numbers of the tessages handled, in the order their handlers ended.</summary>
   protected IReadOnlyList<int> HandledNumbers => [.. _handled];

   protected int TriesOfTheFirst => _triesOfTheFirst;

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();
      await _host.AwaitEndpointsHaveMetEachOtherAsync();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   protected async Task SendInOneUnitOfWorkAsync(params IExactlyOnceTommand[] steps) =>
      await _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWorkAsync(async unitOfWork =>
      {
         foreach(var step in steps)
            await unitOfWork.Resolve<IUnitOfWorkTommandSender>().SendAsync(step, CancellationToken.None);
      });

   protected async Task SendFromTheOtherEndpointAsync(params IExactlyOnceTommand[] tommands)
   {
      foreach(var tommand in tommands)
         await _otherEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(tommand);
   }

   protected async Task PublishFromTheOtherEndpointAsync(params IExactlyOnceTevent[] tevents)
   {
      foreach(var tevent in tevents)
         await _otherEndpoint.ServiceLocator.Resolve<IIndependentTeventPublisher>().PublishAsync(tevent);
   }

   protected async Task AwaitHandledCountAsync(int count) => await HandledGate.AwaitPassedThroughCountEqualToAsync(count, HandlingTimeout);

   protected async Task AwaitTheFirstsFirstTryFailingAsync() => await TheFirstsFailedTriesGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);

   protected async Task AwaitTheFirstsHandlerWaitingAsync() => await TheFirstsHandlerWaitsGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);

   async Task HandleAsync(INumberedTessage tessage)
   {
      if(tessage.Number == 1)
      {
         if(Interlocked.Increment(ref _triesOfTheFirst) == 1) _theFirstsFirstTryStartedAt = Stopwatch.GetTimestamp();
         if(HandledGate.Passed == 0 && Stopwatch.GetElapsedTime(_theFirstsFirstTryStartedAt) < HowLongTheFirstsTriesFailWhileNoOtherIsHandled)
         {
            await TheFirstsFailedTriesGate.AwaitPassThroughAsync();
            throw new IntentionalFailureOfTheFirstException();
         }
         if(HowLongTheFirstsHandlerWaitsForAnotherToBeHandled != WaitTimeout.Zero)
         {
            await TheFirstsHandlerWaitsGate.AwaitPassThroughAsync();
            await HandledGate.TryAwaitPassedThroughCountEqualToAsync(1, HowLongTheFirstsHandlerWaitsForAnotherToBeHandled);
         }
      }

      _handled.Enqueue(tessage.Number);
      await HandledGate.AwaitPassThroughAsync();
   }

   public class when_a_unit_of_work_sends_it_a_step_whose_deferred_handler_gives_up_its_place_and_another_sends_it_a_second_while_the_first_steps_handler_waits_a_second_for_another_to_be_handled
      : Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order
   {
      protected override async Task InitializeAsyncInternal()
      {
         HowLongTheFirstsHandlerWaitsForAnotherToBeHandled = WaitTimeout.Seconds(1);
         await base.InitializeAsyncInternal();
         await SendInOneUnitOfWorkAsync(new StepGivingUpItsPlace(1));
         await AwaitTheFirstsHandlerWaitingAsync();
         await SendInOneUnitOfWorkAsync(new StepGivingUpItsPlace(2));
         await AwaitHandledCountAsync(2);
      }

      //SQLite has one writer, which the first step's handling transaction holds from its claim of the step on: the second step's
      //send waits for it.
      [PCT] public void a_server_engine_handles_the_second_step_while_the_first_steps_handler_runs_and_sqlite_once_it_has_ended() =>
         HandledNumbers.Must().SequenceEqual(TestEnv.SqlLayer.ValueFor<int[]>(msSql: [2, 1], mySql: [2, 1], pgSql: [2, 1], sqlite: [1, 2], sqliteMemory: [1, 2]));
   }

   public class when_a_step_whose_deferred_handler_gives_up_its_place_fails_until_another_is_handled_and_a_second_such_step_is_sent_once_its_first_try_has_failed
      : Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order
   {
      protected override async Task InitializeAsyncInternal()
      {
         HowLongTheFirstsTriesFailWhileNoOtherIsHandled = HandlingTimeout;
         await base.InitializeAsyncInternal();
         await SendInOneUnitOfWorkAsync(new StepGivingUpItsPlace(1));
         await AwaitTheFirstsFirstTryFailingAsync();
         await SendInOneUnitOfWorkAsync(new StepGivingUpItsPlace(2));
         await AwaitHandledCountAsync(2);
      }

      [PCT] public void the_second_step_is_handled_first() => HandledNumbers.Must().SequenceEqual([2, 1]);

      [PCT] public void the_first_step_is_tried_until_it_is_handled() => TriesOfTheFirst.Must().BeGreaterThan(1);
   }

   public class when_a_step_whose_deferred_handler_keeps_its_place_fails_until_another_is_handled_and_a_step_whose_deferred_handler_gives_it_up_is_sent_once_its_first_try_has_failed
      : Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order
   {
      protected override async Task InitializeAsyncInternal()
      {
         HowLongTheFirstsTriesFailWhileNoOtherIsHandled = HandlingTimeout;
         await base.InitializeAsyncInternal();
         await SendInOneUnitOfWorkAsync(new StepKeepingItsPlace(1));
         await AwaitTheFirstsFirstTryFailingAsync();
         await SendInOneUnitOfWorkAsync(new StepGivingUpItsPlace(2));
         await AwaitHandledCountAsync(2);
      }

      [PCT] public void the_step_whose_handler_gives_up_its_place_is_handled_first() => HandledNumbers.Must().SequenceEqual([2, 1]);
   }

   public class when_a_tommand_from_the_other_endpoint_whose_handler_gives_up_its_place_fails_until_another_is_handled_and_a_second_such_tommand_is_sent_once_its_first_try_has_failed
      : Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order
   {
      protected override async Task InitializeAsyncInternal()
      {
         HowLongTheFirstsTriesFailWhileNoOtherIsHandled = HandlingTimeout;
         await base.InitializeAsyncInternal();
         await SendFromTheOtherEndpointAsync(new TommandGivingUpItsPlace(1));
         await AwaitTheFirstsFirstTryFailingAsync();
         await SendFromTheOtherEndpointAsync(new TommandGivingUpItsPlace(2));
         await AwaitHandledCountAsync(2);
      }

      [PCT] public void the_second_tommand_is_handled_first() => HandledNumbers.Must().SequenceEqual([2, 1]);
   }

   public class when_a_tevent_from_the_other_endpoint_whose_handlers_all_give_up_their_place_fails_until_another_is_handled_and_a_second_such_tevent_is_published_once_its_first_try_has_failed
      : Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order
   {
      protected override async Task InitializeAsyncInternal()
      {
         HowLongTheFirstsTriesFailWhileNoOtherIsHandled = HandlingTimeout;
         await base.InitializeAsyncInternal();
         await PublishFromTheOtherEndpointAsync(new TeventWhoseHandlersAllGiveUpTheirPlace(1));
         await AwaitTheFirstsFirstTryFailingAsync();
         await PublishFromTheOtherEndpointAsync(new TeventWhoseHandlersAllGiveUpTheirPlace(2));
         await AwaitHandledCountAsync(2);
      }

      [PCT] public void the_second_tevent_is_handled_first() => HandledNumbers.Must().SequenceEqual([2, 1]);
   }

   public class when_a_tevent_from_the_other_endpoint_one_of_whose_handlers_keeps_its_place_fails_for_a_second_and_a_second_such_tevent_is_published_once_its_first_try_has_failed
      : Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order
   {
      protected override async Task InitializeAsyncInternal()
      {
         HowLongTheFirstsTriesFailWhileNoOtherIsHandled = TimeSpan.FromSeconds(1);
         await base.InitializeAsyncInternal();
         await PublishFromTheOtherEndpointAsync(new TeventOneOfWhoseHandlersKeepsItsPlace(1));
         await AwaitTheFirstsFirstTryFailingAsync();
         await PublishFromTheOtherEndpointAsync(new TeventOneOfWhoseHandlersKeepsItsPlace(2));
         await AwaitHandledCountAsync(2);
      }

      [PCT] public void the_second_tevent_is_handled_once_the_first_has_been() => HandledNumbers.Must().SequenceEqual([1, 2]);
   }

   class IntentionalFailureOfTheFirstException() : Exception("Intentional failure of a try of number 1's handling, while no other tessage has been handled.");

   class TheOtherEndpoint : ExactlyOnceEndpointDeclaration<TheOtherEndpoint>, IEndpointIdentity
   {
      public static string Name => "HandlingOrderSender";
      public static EndpointId Id => new(Guid.Parse("4C8E2A17-5B93-4F60-8D21-E7A0C3B59F48"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();
   }

   class TheEndpoint : ExactlyOnceEndpointDeclaration<TheEndpoint>, IEndpointIdentity
   {
      public static string Name => "HandlingOrderHandlers";
      public static EndpointId Id => new(Guid.Parse("B1D7F340-2E86-4A5C-9F17-6C0A8E3D52B9"));

      readonly Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order _specification;
      internal TheEndpoint(Given_an_exactly_once_endpoint_whose_handlers_give_up_their_place_in_the_handling_order specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();

      protected override void RegisterDeferredTommandHandlers(IDeferredTommandHandlerRegistrar handle) => handle
         .ForTommand((StepGivingUpItsPlace step, IUnitOfWorkResolver _) => _specification.HandleAsync(step), HandlingOrder.None)
         .ForTommand((StepKeepingItsPlace step, IUnitOfWorkResolver _) => _specification.HandleAsync(step));

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((TommandGivingUpItsPlace tommand, IUnitOfWorkResolver _) => _specification.HandleAsync(tommand), HandlingOrder.None);

      protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) => handle
         .ForTevent((ITeventWhoseHandlersAllGiveUpTheirPlace tevent, IUnitOfWorkResolver _) => _specification.HandleAsync(tevent), HandlingOrder.None)
         .ForTevent((ITeventWhoseHandlersAllGiveUpTheirPlace _, IUnitOfWorkResolver _) => Task.CompletedTask, HandlingOrder.None)
         .ForTevent((ITeventOneOfWhoseHandlersKeepsItsPlace tevent, IUnitOfWorkResolver _) => _specification.HandleAsync(tevent), HandlingOrder.None)
         .ForTevent((ITeventOneOfWhoseHandlersKeepsItsPlace _, IUnitOfWorkResolver _) => Task.CompletedTask);
   }
}
