using System.Collections.Concurrent;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Typermedia.Client;
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

public abstract class EndpointHostTestBase : UniversalTestBase
{
   static readonly WaitTimeout _timeout = WaitTimeout.Seconds(30);

   ///<summary>The Backend endpoint's identity. Fixed, not generated: an endpoint keeps its identity — and thereby its pooled<br/>
   /// database — across host rebuilds, which is what lets specs script an endpoint's restart.</summary>
   protected static readonly EndpointId BackendEndpointId = new(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6"));

   ///<summary>The Remote endpoint's identity. Fixed for the same reason as <see cref="BackendEndpointId"/>.</summary>
   protected static readonly EndpointId RemoteEndpointId = new(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B"));

   ///<summary>The identity of the successor to the Remote endpoint (see<br/>
   /// <see cref="StartHostWithTheBackendEndpointAndASuccessorToTheRemoteEndpointAsync"/>) — deliberately a NEW identity:<br/>
   /// a blue/green replacement is a different endpoint that advertises the same tommand type, never the old identity reused.</summary>
   protected static readonly EndpointId RemoteSuccessorEndpointId = new(Guid.Parse("46ECC3A4-5657-4A0A-9C78-9FEEA5A1010D"));

   protected ITestingEndpointHost Host { get; private set; } = null!;
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

   protected IEndpoint BackendEndPoint { get; private set; } = null!;
   TypermediaTestClient Client { get; set; } = null!;
   protected IRemoteTypermediaNavigator Navigator => Client.Navigator;
   protected IEndpoint RemoteEndpoint { get; private set; } = null!;
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
      Host = TestingEndpointHost.Create(_rootContainer, new ExactlyOnceTessagingTestingEndpointHostFeature(), new DistributedTypermediaTestingEndpointHostFeature());

      BackendEndPoint = Host.RegisterEndpoint(
         "Backend",
         BackendEndpointId,
         builder =>
         {
            builder.TypeMapper.RegisterCommonTestTypeMappings();

            builder.RegisterTeventStore()
                   .HandleTaggregate<MyTaggregate, IMyTaggregateTevent>();

            //Exactly-once kinds are async end to end, so their handlers are declared async; the gates themselves are synchronous, so the bodies complete their tasks synchronously.
            builder.RegisterTessageHandlers(handle => handle
                      .ForTommand((MyExactlyOnceTommand _) =>
                       {
                          MyExactlyOnceTommandHandlerThreadGate.AwaitPassThrough();
                          return Task.CompletedTask;
                       })
                      .ForTevent((IMyExactlyOnceTevent _) =>
                       {
                          TeventHandlerThreadGate.AwaitPassThrough();
                          return Task.CompletedTask;
                       })
                      .ForTevent((IMyTaggregateTevent _) =>
                       {
                          MyLocalTaggregateTeventHandlerThreadGate.AwaitPassThrough();
                          return Task.CompletedTask;
                       })
                      .ForTevent((IMyBestEffortTevent _) => MyBestEffortTeventLocalHandlerThreadGate.AwaitPassThrough())
                      .ForTommand((MyCreateTaggregateTommand tommand, ILocalTypermediaNavigatorSession navigator) =>
                       {
                          MyCreateTaggregateTommandHandlerThreadGate.AwaitPassThrough();
                          MyTaggregate.Create(tommand.TaggregateId, navigator);
                       })
                      .ForTommand((MyUpdateTaggregateTommand tommand, ILocalTypermediaNavigatorSession navigator) =>
                       {
                          MyUpdateTaggregateTommandHandlerThreadGate.AwaitPassThrough();
                          navigator.Execute(new TeventStoreApi().Tueries.GetForUpdate<MyTaggregate>(tommand.TaggregateId)).Update();
                       })
                      .ForTuery((MyTuery _) =>
                       {
                          TueryHandlerThreadGate.AwaitPassThrough();
                          return new MyTueryResult();
                       })
                      .ForTommand((MyAtMostOnceTypermediaTommandWithResult _) =>
                       {
                          TommandHandlerWithResultThreadGate.AwaitPassThrough();
                          return new MyTommandResult();
                       }));

            //Observation - the transaction-ignoring subscription kind: fires at publish time for the Backend's own locally published tevents.
            builder.ObserveTevents(observe => observe
                      .ForTevent((IMyTaggregateTevent _) => MyTaggregateTeventBackendObserverThreadGate.AwaitPassThrough()));
         });
   }

   void RegisterRemoteEndpoint(bool withItsTommandHandler = true) =>
      RemoteEndpoint = Host.RegisterEndpoint("Remote",
                                             RemoteEndpointId,
                                             builder =>
                                             {
                                                builder.TypeMapper.RegisterCommonTestTypeMappings();

                                                if(withItsTommandHandler)
                                                {
                                                   builder.RegisterTessageHandlers(handle => handle
                                                             .ForTommand((MyExactlyOnceTommandHandledByTheRemoteEndpoint _) =>
                                                              {
                                                                 MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.AwaitPassThrough();
                                                                 return Task.CompletedTask;
                                                              }));
                                                }

                                                builder.RegisterTessageHandlers(handle => handle
                                                          .ForTevent((IMyTaggregateTevent _) =>
                                                           {
                                                              MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassThrough();
                                                              return Task.CompletedTask;
                                                           })
                                                          //Publisher-conscious subscription: subscribing to the taggregate's wrapper type receives the wrapped tevent as MyTaggregate published it.
                                                          .ForTevent((IMyTaggregateTevent<IMyTaggregateTevent> _) =>
                                                           {
                                                              MyRemotePublisherConsciousTeventHandlerThreadGate.AwaitPassThrough();
                                                              return Task.CompletedTask;
                                                           })
                                                          .ForTevent((IMyBestEffortTevent tevent) =>
                                                           {
                                                              RemotelyReceivedBestEffortTevents.Enqueue(tevent);
                                                              MyBestEffortTeventRemoteHandlerThreadGate.AwaitPassThrough();
                                                           }));

                                                //Observation - the transaction-ignoring subscription kind: fires on arrival, before and outside the transactional handling above.
                                                builder.ObserveTevents(observe => observe
                                                          .ForTevent((IMyTaggregateTevent _) => MyTaggregateTeventRemoteObserverThreadGate.AwaitPassThrough())
                                                          .ForTevent((IMyBestEffortTevent _) => MyBestEffortTeventRemoteObserverThreadGate.AwaitPassThrough()));
                                             });

   protected async Task StartHostAsync()
   {
      CreateHostAndRegisterBackendEndpoint();
      RegisterRemoteEndpoint();
      await StartHostAndConnectClientAsync();
   }

   ///<summary>Starts a host containing only the Backend endpoint — the Remote endpoint is down. An endpoint keeps its identity<br/>
   /// and database across host rebuilds (see <see cref="BackendEndpointId"/>), so pairing this with <see cref="StartHostAsync"/><br/>
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
   /// <c>dev_docs/TODO/WIP/Tessaging/durable-peer-topology.md</c>).</summary>
   protected async Task StartHostWithTheRemoteEndpointReturningNoLongerHandlingItsTommandAsync()
   {
      CreateHostAndRegisterBackendEndpoint();
      RegisterRemoteEndpoint(withItsTommandHandler: false);
      await StartHostAndConnectClientAsync();
   }

   ///<summary>Starts a host containing the Backend endpoint and a successor to the Remote endpoint: a NEW endpoint identity<br/>
   /// (<see cref="RemoteSuccessorEndpointId"/>) whose advertisement handles <see cref="MyExactlyOnceTommandHandledByTheRemoteEndpoint"/> —<br/>
   /// the blue/green replacement shape: the predecessor retired, and a different endpoint advertises the same tommand type.</summary>
   protected async Task StartHostWithTheBackendEndpointAndASuccessorToTheRemoteEndpointAsync()
   {
      CreateHostAndRegisterBackendEndpoint();
      RemoteEndpoint = null!; //There is no Remote endpoint in this host either: the successor replaces it under its own, new identity.
      Host.RegisterEndpoint("RemoteSuccessor",
                            RemoteSuccessorEndpointId,
                            builder =>
                            {
                               builder.TypeMapper.RegisterCommonTestTypeMappings();

                               builder.RegisterTessageHandlers(handle => handle
                                         .ForTommand((MyExactlyOnceTommandHandledByTheRemoteEndpoint _) =>
                                          {
                                             RemoteSuccessorTommandHandlerThreadGate.AwaitPassThrough();
                                             return Task.CompletedTask;
                                          }));
                            });
      await StartHostAndConnectClientAsync();
   }

   async Task StartHostAndConnectClientAsync()
   {
      await Host.StartAsync();
      Client = await TypermediaTestClient.ConnectTo(BackendEndPoint.TypermediaAddress!, mapper => mapper.RegisterCommonTestTypeMappings());
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());

   protected void PublishBestEffortTeventOnTheBackendInATransaction(int sequenceNumber) =>
      BackendEndPoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));
}
