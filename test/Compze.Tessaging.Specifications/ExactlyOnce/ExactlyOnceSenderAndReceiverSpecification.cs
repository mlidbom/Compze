using System.Collections.Concurrent;
using System.Diagnostics;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Underscore;

namespace Compze.Tessaging.Specifications.ExactlyOnce;

///<summary>Two exactly-once endpoints: a Sender with no handlers, and a Receiver handling <see cref="NumberedExactlyOnceTommand"/><br/>
/// with the retry policy a specification declares, whose tries of the first tommand fail as the specification scripts.<br/>
/// Each endpoint keeps its identity and its pooled database across host rebuilds, so a specification can stop the Receiver<br/>
/// and start it again.</summary>
public abstract class ExactlyOnceSenderAndReceiverSpecification : UniversalTestBase
{
   static readonly WaitTimeout GateTimeout = WaitTimeout.Seconds(30);

   readonly RetryPolicy? _receiversRetryPolicy;
   readonly IDependencyInjectionContainer _rootContainer;
   readonly ConcurrentQueue<HandlerTry> _tries = new();
   readonly ConcurrentQueue<HandledTommand> _handled = new();
   TestingEndpointHost? _host;

   ///<summary>Passed through once by each try of a tommand's handling, failed or not.</summary>
   protected IThreadGate TriesGate { get; } = IThreadGate.NewOpen(GateTimeout, nameof(TriesGate));

   ///<summary>Passed through once by each tommand handled.</summary>
   protected IThreadGate HandledGate { get; } = IThreadGate.NewOpen(GateTimeout, nameof(HandledGate));

   ///<summary>How many tries of the first tommand's handling fail, from the first try on: <see cref="int.MaxValue"/> keeps failing.</summary>
   protected int TriesOfTheFirstTommandThatFail { get; set; }

   ///<param name="receiversRetryPolicy">The Receiver's declared retry policy; null declares none, for the default.</param>
   protected ExactlyOnceSenderAndReceiverSpecification(RetryPolicy? receiversRetryPolicy)
   {
      _receiversRetryPolicy = receiversRetryPolicy;
      _rootContainer = TestEnv.DIContainer.CreateTestingContainerBuilder()
                              ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer())
                              .Build();
   }

   protected IReadOnlyList<HandlerTry> Tries => [.. _tries];
   protected IReadOnlyList<int> HandledTommandNumbers => [.. _handled.Select(it => it.Number)];
   protected HandledTommand HandlingOf(int tommandNumber) => _handled.Single(it => it.Number == tommandNumber);
   protected IReadOnlyList<HandlerTry> TriesOf(int tommandNumber) => [.. _tries.Where(it => it.TommandNumber == tommandNumber)];

   ExactlyOnceEndpoint Sender { get; set; } = null!;
   ExactlyOnceEndpoint Receiver { get; set; } = null!;

   ///<summary>Builds a host of the Sender and the Receiver over the endpoints' databases, starts it, and awaits the endpoints<br/>
   /// meeting, so that a tommand the Sender sends binds to the Receiver at once.</summary>
   protected async Task StartHostAsync()
   {
      _host = TestingEndpointHost.Create(_rootContainer);
      Sender = _host.RegisterEndpoint(new SenderDeclaration());
      Receiver = _host.RegisterEndpoint(new ReceiverDeclaration(this));
      await _host.StartAsync();
      await _host.AwaitEndpointsHaveMetEachOtherAsync();
   }

   ///<summary>Completes once the Receiver has begun stopping: its <see cref="Endpoint.IsRunning"/> turns false as its stop<br/>
   /// begins, before it stops sending, stops listening and drains its inbox. Throws a <see cref="TimeoutException"/> if the<br/>
   /// Receiver is still running after <paramref name="timeout"/>.</summary>
   protected async Task AwaitTheReceiverHasBegunStoppingAsync(WaitTimeout timeout)
   {
      var waitStartedAt = Stopwatch.GetTimestamp();
      while(Receiver.IsRunning)
      {
         if(Stopwatch.GetElapsedTime(waitStartedAt) > timeout) throw new TimeoutException($"The Receiver was still running {timeout} after the wait for its stop began.");
         await Task.Delay(TimeSpan.FromMilliseconds(1));
      }
   }

   protected async Task SendAsync(int tommandNumber) =>
      await Sender.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new NumberedExactlyOnceTommand(tommandNumber));

   ///<summary>Disposes the host once its endpoints are at rest, as the testing host does, and answers what its disposal threw.</summary>
   protected async Task<AggregateException?> DisposeHostAsync() => await WhatDisposingTheHostThrewAsync(host => host.DisposeAsync().AsTask());

   ///<summary>Disposes the host without waiting for its endpoints to come to rest, for a specification that stops the Receiver<br/>
   /// while a tommand is still being tried, or waits to be tried again, and answers what its disposal threw.</summary>
   protected async Task<AggregateException?> DisposeHostWithoutWaitingForItsEndpointsToBeAtRestAsync() =>
      await WhatDisposingTheHostThrewAsync(host => host.DisposeWithoutWaitingForEndpointsToBeAtRestAsync());

   async Task<AggregateException?> WhatDisposingTheHostThrewAsync(Func<TestingEndpointHost, Task> dispose)
   {
      var host = _host!;
      _host = null;
      try
      {
         await dispose(host);
         return null;
      }
      catch(AggregateException exception)
      {
         return exception;
      }
   }

   ///<summary>The numbers of the tommands whose handler's failures <paramref name="hostDisposalException"/> reports, in the<br/>
   /// order it reports them: none when the host's disposal threw nothing.</summary>
   protected static IReadOnlyList<int> NumbersOfTheTommandsWhoseFailuresAreReportedIn(AggregateException? hostDisposalException) =>
      hostDisposalException is null
         ? []
         : [.. hostDisposalException.Flatten().InnerExceptions.OfType<IntentionalHandlerFailureException>().Select(it => it.TommandNumber)];

   protected override async Task DisposeAsyncInternal()
   {
      if(_host != null) await _host.DisposeAsync();
      await _rootContainer.DisposeAsync();
   }

   async Task HandleAsync(NumberedExactlyOnceTommand tommand)
   {
      //The Receiver handles one exactly-once tessage at a time, so no other try runs while this one counts.
      var tryNumber = _tries.Count(it => it.TommandNumber == tommand.Number) + 1;
      _tries.Enqueue(new HandlerTry(tommand.Number, tryNumber));
      await TriesGate.AwaitPassThroughAsync();

      if(tommand.Number == 1 && tryNumber <= TriesOfTheFirstTommandThatFail) throw new IntentionalHandlerFailureException(tommand.Number, tryNumber);

      _handled.Enqueue(new HandledTommand(tommand.Number));
      await HandledGate.AwaitPassThroughAsync();
   }

   ///<summary>One try of a tommand's handling, as its handler began it.</summary>
   protected class HandlerTry(int tommandNumber, int tryNumber)
   {
      public int TommandNumber { get; } = tommandNumber;
      public int TryNumber { get; } = tryNumber;
      public long StartedAt { get; } = Stopwatch.GetTimestamp();
   }

   ///<summary>A tommand's handler completing.</summary>
   protected class HandledTommand(int number)
   {
      public int Number { get; } = number;
      public long HandledAt { get; } = Stopwatch.GetTimestamp();
   }

   class SenderDeclaration : ExactlyOnceEndpointDeclaration<SenderDeclaration>, IEndpointIdentity
   {
      public static string Name => "Sender";
      public static EndpointId Id => new(Guid.Parse("3F6B9E21-7C40-4D85-A1E3-92B5C07D4E18"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();
   }

   class ReceiverDeclaration : ExactlyOnceEndpointDeclaration<ReceiverDeclaration>, IEndpointIdentity
   {
      public static string Name => "Receiver";
      public static EndpointId Id => new(Guid.Parse("A8D24F57-1B93-4E06-8C7A-5D31E9F02B64"));

      readonly ExactlyOnceSenderAndReceiverSpecification _specification;
      internal ReceiverDeclaration(ExactlyOnceSenderAndReceiverSpecification specification) => _specification = specification;

      protected override RetryPolicy? RetryPolicy => _specification._receiversRetryPolicy;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((NumberedExactlyOnceTommand tommand, IUnitOfWorkResolver _) => _specification.HandleAsync(tommand));
   }
}
