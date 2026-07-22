using System.Collections.Concurrent;
using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tests.Infrastructure;
using Compze.Teventive.TeventStore.Typermedia;
using Compze.Underscore;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Tessaging.Typermedia;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public abstract partial class EndpointHostTestBase : UniversalTestBase
{
   static readonly WaitTimeout _timeout = WaitTimeout.Seconds(30);

   protected TestingEndpointHost Host { get; private set; } = null!;
   public IThreadGate MyExactlyOnceTommandHandlerThreadGate { get; }
   public IThreadGate MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate { get; }
   public IThreadGate RemoteSuccessorTommandHandlerThreadGate { get; }
   public IThreadGate TommandHandlerWithResultThreadGate { get; }
   public IThreadGate MyCreateTaggregateTommandHandlerThreadGate { get; }
   public IThreadGate MyUpdateTaggregateTommandHandlerThreadGate { get; }
   public IThreadGate MyRemoteTaggregateTeventHandlerThreadGate { get; }
   public IThreadGate MyRemotePublisherConsciousTeventHandlerThreadGate { get; }
   public IThreadGate MyLocalTaggregateTeventHandlerThreadGate { get; }
   public IThreadGate MyBestEffortTeventLocalHandlerThreadGate { get; }
   public IThreadGate MyBestEffortTeventRemoteHandlerThreadGate { get; }
   public IThreadGate MyTaggregateTeventBackendObserverThreadGate { get; }
   public IThreadGate MyTaggregateTeventRemoteObserverThreadGate { get; }
   public IThreadGate MyBestEffortTeventRemoteObserverThreadGate { get; }
   public IThreadGate TeventHandlerThreadGate { get; }
   public IThreadGate TueryHandlerThreadGate { get; }

   ///<summary>Every <see cref="IMyBestEffortTevent"/> the Remote endpoint's handler has received, in handling order — best-effort delivery promises in-order arrival within a connected session.</summary>
   protected ConcurrentQueue<IMyBestEffortTevent> RemotelyReceivedBestEffortTevents { get; } = new();

   IReadOnlyList<IThreadGate> AllGates  { get; }

   protected ExactlyOnceEndpoint BackendEndPoint { get; private set; } = null!;
   TypermediaTestClient Client { get; set; } = null!;
   protected IRemoteTypermediaNavigator Navigator => Client.Navigator;
   protected ExactlyOnceEndpoint RemoteEndpoint { get; private set; } = null!;
   IDependencyInjectionContainer? _rootContainer;

   protected EndpointHostTestBase()
   {

      AllGates =
      [
         MyExactlyOnceTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyExactlyOnceTommandHandlerThreadGate)),
         MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate)),
         RemoteSuccessorTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(RemoteSuccessorTommandHandlerThreadGate)),
         TommandHandlerWithResultThreadGate = IThreadGate.NewOpen(_timeout, nameof(TommandHandlerWithResultThreadGate)),
         MyCreateTaggregateTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyCreateTaggregateTommandHandlerThreadGate)),
         MyUpdateTaggregateTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyUpdateTaggregateTommandHandlerThreadGate)),
         MyRemoteTaggregateTeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyRemoteTaggregateTeventHandlerThreadGate)),
         MyRemotePublisherConsciousTeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyRemotePublisherConsciousTeventHandlerThreadGate)),
         MyLocalTaggregateTeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyLocalTaggregateTeventHandlerThreadGate)),
         MyBestEffortTeventLocalHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyBestEffortTeventLocalHandlerThreadGate)),
         MyBestEffortTeventRemoteHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyBestEffortTeventRemoteHandlerThreadGate)),
         MyTaggregateTeventBackendObserverThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyTaggregateTeventBackendObserverThreadGate)),
         MyTaggregateTeventRemoteObserverThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyTaggregateTeventRemoteObserverThreadGate)),
         MyBestEffortTeventRemoteObserverThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyBestEffortTeventRemoteObserverThreadGate)),
         TeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(TeventHandlerThreadGate)),
         TueryHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(TueryHandlerThreadGate))
      ];
   }

   protected override async Task InitializeAsyncInternal() => await StartHostAsync();

   protected override async Task DisposeAsyncInternal()
   {
      OpenGates();
      await Client.DisposeAsync();
      await Host.DisposeAsync();
      if(_rootContainer != null)
      {
         await _rootContainer.DisposeAsync();
         _rootContainer = null;
      }
   }

   static IContainerBuilder CreateRootBuilder() =>
      TestEnv.DIContainer.CreateTestingContainerBuilder()
             ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer());

   void CreateHostAndRegisterBackendEndpoint()
   {
      _rootContainer ??= CreateRootBuilder().Build();
      Host = TestingEndpointHost.Create(_rootContainer);
      BackendEndPoint = Host.RegisterEndpoint(new BackendEndpointDeclaration(this));
   }

   void RegisterRemoteEndpoint(bool withItsTommandHandler = true, bool withItsTeventSubscriptions = true) =>
      RemoteEndpoint = Host.RegisterEndpoint(new RemoteEndpointDeclaration(this, withItsTommandHandler, withItsTeventSubscriptions));

   protected async Task StartHostAsync()
   {
      CreateHostAndRegisterBackendEndpoint();
      RegisterRemoteEndpoint();
      await StartHostAndConnectClientAsync();
   }

   ///<summary>Starts a host containing only the Backend endpoint — the Remote endpoint is down. An endpoint keeps its identity<br/>
   /// and database across host rebuilds (see <see cref="BackendEndpointDeclaration"/>), so pairing this with <see cref="StartHostAsync"/><br/>
   /// scripts the Remote endpoint's downtime: rebuild the host without it, later rebuild it back in.</summary>
   protected async Task StartHostWithOnlyTheBackendEndpointAsync()
   {
      CreateHostAndRegisterBackendEndpoint();
      RemoteEndpoint = null!; //There is no Remote endpoint in this host: touching it must fail loudly, not answer from the previous host's disposed instance.
      await StartHostAndConnectClientAsync();
   }

   ///<summary>Starts a host in which the Remote endpoint returns with a shrunk advertisement: it no longer handles<br/>
   /// <see cref="MyExactlyOnceTommandHandledByTheRemoteEndpoint"/>, while every tevent subscription remains — the deployment<br/>
   /// where a handler was removed from an endpoint that keeps its identity (see the advertisement lifecycle in<br/>
   /// <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
   protected async Task StartHostWithTheRemoteEndpointReturningNoLongerHandlingItsTommandAsync()
   {
      CreateHostAndRegisterBackendEndpoint();
      RegisterRemoteEndpoint(withItsTommandHandler: false);
      await StartHostAndConnectClientAsync();
   }

   ///<summary>The mirror of <see cref="StartHostWithTheRemoteEndpointReturningNoLongerHandlingItsTommandAsync"/>: the Remote<br/>
   /// endpoint returns having renounced every tevent subscription — its advertisement keeps only the tommand handler — the<br/>
   /// deployment where an endpoint keeping its identity stopped subscribing.</summary>
   protected async Task StartHostWithTheRemoteEndpointReturningHavingRenouncedItsTeventSubscriptionsAsync()
   {
      CreateHostAndRegisterBackendEndpoint();
      RegisterRemoteEndpoint(withItsTeventSubscriptions: false);
      await StartHostAndConnectClientAsync();
   }

   ///<summary>Starts a host containing the Backend endpoint and a successor to the Remote endpoint: a NEW endpoint identity<br/>
   /// (<see cref="RemoteSuccessorEndpointDeclaration"/>) whose advertisement handles <see cref="MyExactlyOnceTommandHandledByTheRemoteEndpoint"/> —<br/>
   /// the blue/green replacement shape: the predecessor retired, and a different endpoint advertises the same tommand type.</summary>
   protected async Task StartHostWithTheBackendEndpointAndASuccessorToTheRemoteEndpointAsync()
   {
      CreateHostAndRegisterBackendEndpoint();
      RemoteEndpoint = null!; //There is no Remote endpoint in this host either: the successor replaces it under its own, new identity.
      Host.RegisterEndpoint(new RemoteSuccessorEndpointDeclaration(this));
      await StartHostAndConnectClientAsync();
   }

   async Task StartHostAndConnectClientAsync()
   {
      await Host.StartAsync();
      //These specs' very next act rides the endpoints having discovered each other - tevent fan-out membership, peer-memory
      //assertions, instant binds within the deliberately short handler-availability patience - so the mutual first contact is
      //awaited explicitly instead of racing the reconciliation.
      await Host.AwaitEndpointsHaveMetEachOtherAsync();
      Client = await TypermediaTestClient.ConnectTo(BackendEndPoint.Address!, registrar => registrar.RequireCommonTestTypeMappings());
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());

   protected void PublishBestEffortTeventOnTheBackendInATransaction(int sequenceNumber) =>
      BackendEndPoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));
}
