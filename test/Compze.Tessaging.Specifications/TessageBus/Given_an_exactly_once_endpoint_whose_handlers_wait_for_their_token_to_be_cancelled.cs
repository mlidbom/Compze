using System.Collections.Concurrent;
using System.Diagnostics;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.TessageBus;

///<summary>The token a caller gives a send or a publish reaches the handlers that run in the caller's execution: cancelling it<br/>
/// cancels them, and the send or publish fails with an <see cref="OperationCanceledException"/>. It also ends the wait for a<br/>
/// receiver to bind a tommand to.</summary>
public abstract class Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled : UniversalTestBase
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);

   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _endpoint;
   readonly IThreadGate _handlersStarted = IThreadGate.NewOpen(HandlingTimeout, nameof(_handlersStarted));
   readonly ConcurrentQueue<string> _cancelledHandlers = new();

   protected Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled()
   {
      _host = TestingEndpointHost.Create();
      _endpoint = _host.RegisterEndpoint(new TheEndpoint(this));
   }

   ///<summary>The tessage types whose handlers gave up when the token they were handed was cancelled.</summary>
   protected IReadOnlyList<string> TypesOfTheTessagesWhoseHandlersWereCancelled => [.. _cancelledHandlers];

   protected IIndependentTommandSender IndependentTommandSender => _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>();
   protected IIndependentTeventPublisher IndependentTeventPublisher => _endpoint.ServiceLocator.Resolve<IIndependentTeventPublisher>();

   protected async Task InUnitOfWorkAsync(Func<IUnitOfWorkResolver, Task> work) =>
      await _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWorkAsync(work);

   ///<summary>Runs <paramref name="sendOrPublish"/> with a token, cancels the token once a handler has begun waiting for its<br/>
   /// cancellation, and answers the <see cref="OperationCanceledException"/> the send or publish then threw: null when it threw<br/>
   /// none.</summary>
   protected async Task<OperationCanceledException?> WhatItThrowsWhenItsTokenIsCancelledWhileTheHandlerWaitsAsync(Func<CancellationToken, Task> sendOrPublish)
   {
      using var token = new CancellationTokenSource();
      var running = sendOrPublish(token.Token);
      await _handlersStarted.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
      await token.CancelAsync();
      try
      {
         await running;
         return null;
      }
      catch(OperationCanceledException exception)
      {
         return exception;
      }
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   async Task WaitForTheCancellationAsync(object tessage, CancellationToken cancellationToken)
   {
      await _handlersStarted.AwaitPassThroughAsync();
      try
      {
         await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
      }
      catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
      {
         _cancelledHandlers.Enqueue(tessage.GetType().Name);
         throw;
      }
      throw new TimeoutException($"The token handed to the handler of {tessage.GetType().Name} was not cancelled within a minute.");
   }

   public class when_an_independent_send_of_a_tommand_it_handles_has_its_token_cancelled_while_the_handler_waits : Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled
   {
      OperationCanceledException? _whatTheSendThrew;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _whatTheSendThrew = await WhatItThrowsWhenItsTokenIsCancelledWhileTheHandlerWaitsAsync(
                                async token => await IndependentTommandSender.SendAsync(new TommandWhoseHandlerWaitsForItsCancellation(), token));
      }

      [PCT] public void the_tommands_handler_is_cancelled() =>
         TypesOfTheTessagesWhoseHandlersWereCancelled.Must().SequenceEqual([nameof(TommandWhoseHandlerWaitsForItsCancellation)]);

      [PCT] public void the_send_throws_an_OperationCanceledException() => _whatTheSendThrew.Must().NotBeNull();
   }

   public class when_a_send_within_a_unit_of_work_of_a_tommand_it_handles_has_its_token_cancelled_while_the_handler_waits : Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled
   {
      OperationCanceledException? _whatTheSendThrew;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _whatTheSendThrew = await WhatItThrowsWhenItsTokenIsCancelledWhileTheHandlerWaitsAsync(
                                async token => await InUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<IUnitOfWorkTommandSender>().SendAsync(new TommandWhoseHandlerWaitsForItsCancellation(), token)));
      }

      [PCT] public void the_tommands_handler_is_cancelled() =>
         TypesOfTheTessagesWhoseHandlersWereCancelled.Must().SequenceEqual([nameof(TommandWhoseHandlerWaitsForItsCancellation)]);

      [PCT] public void the_send_throws_an_OperationCanceledException() => _whatTheSendThrew.Must().NotBeNull();
   }

   public class when_an_independent_publish_of_a_tevent_it_handles_has_its_token_cancelled_while_the_handler_waits : Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled
   {
      OperationCanceledException? _whatThePublishThrew;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _whatThePublishThrew = await WhatItThrowsWhenItsTokenIsCancelledWhileTheHandlerWaitsAsync(
                                   async token => await IndependentTeventPublisher.PublishAsync(new TeventWhoseHandlerWaitsForItsCancellation(), token));
      }

      [PCT] public void the_tevents_handler_is_cancelled() =>
         TypesOfTheTessagesWhoseHandlersWereCancelled.Must().SequenceEqual([nameof(TeventWhoseHandlerWaitsForItsCancellation)]);

      [PCT] public void the_publish_throws_an_OperationCanceledException() => _whatThePublishThrew.Must().NotBeNull();
   }

   public class when_a_publish_within_a_unit_of_work_of_a_tevent_it_handles_has_its_token_cancelled_while_the_handler_waits : Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled
   {
      OperationCanceledException? _whatThePublishThrew;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _whatThePublishThrew = await WhatItThrowsWhenItsTokenIsCancelledWhileTheHandlerWaitsAsync(
                                   async token => await InUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().PublishAsync(new TeventWhoseHandlerWaitsForItsCancellation(), token)));
      }

      [PCT] public void the_tevents_handler_is_cancelled() =>
         TypesOfTheTessagesWhoseHandlersWereCancelled.Must().SequenceEqual([nameof(TeventWhoseHandlerWaitsForItsCancellation)]);

      [PCT] public void the_publish_throws_an_OperationCanceledException() => _whatThePublishThrew.Must().NotBeNull();
   }

   public class when_a_tommand_no_endpoint_handles_is_sent_with_a_cancelled_token : Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled
   {
      TimeSpan _timeTheSendTook;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         var sendStartedAt = Stopwatch.GetTimestamp();
         await InvokingAsync(async () => await IndependentTommandSender.SendAsync(new TommandNoEndpointHandles(), new CancellationToken(canceled: true)))
              .Must().ThrowAsync<OperationCanceledException>();
         _timeTheSendTook = Stopwatch.GetElapsedTime(sendStartedAt);
      }

      //A send whose token didn't reach the wait would wait out the endpoint's 30 second handler-availability patience.
      [PCT] public void the_send_gives_up_without_waiting_for_a_receiver_to_bind_the_tommand_to() => _timeTheSendTook.Must().BeLessThan(TimeSpan.FromSeconds(10));
   }

   class TheEndpoint : ExactlyOnceEndpointDeclaration<TheEndpoint>, IEndpointIdentity
   {
      public static string Name => "CancellableHandlers";
      public static EndpointId Id => new(Guid.Parse("C4E7A2B9-5D18-4F63-9A0E-71B3D6C84F25"));

      readonly Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled _specification;
      internal TheEndpoint(Given_an_exactly_once_endpoint_whose_handlers_wait_for_their_token_to_be_cancelled specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((TommandWhoseHandlerWaitsForItsCancellation tommand, IUnitOfWorkResolver _, CancellationToken cancellationToken) => _specification.WaitForTheCancellationAsync(tommand, cancellationToken));

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((ITeventWhoseHandlerWaitsForItsCancellation tevent, IUnitOfWorkResolver _, CancellationToken cancellationToken) => _specification.WaitForTheCancellationAsync(tevent, cancellationToken));
   }
}
