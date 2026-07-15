using System.Collections.Concurrent;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.Hosting.Testing;
using Compze.Typermedia.Hosting.Testing;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Hosting;
using Compze.Typermedia.Client;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tests.Infrastructure;
using Compze.Teventive.TeventStore.Typermedia;
using Compze.Underscore;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Typermedia;
using Compze.Typermedia.HandlerRegistration;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public abstract class EndpointHostTestBase : UniversalTestBase
{
   static readonly WaitTimeout _timeout = WaitTimeout.Seconds(30);
   protected ITestingEndpointHost Host { get; private set; } = null!;
   public IThreadGate MyExactlyOnceTommandHandlerThreadGate { get; }
   public IThreadGate TommandHandlerWithResultThreadGate { get; }
   public IThreadGate MyCreateTaggregateTommandHandlerThreadGate { get; }
   public IThreadGate MyUpdateTaggregateTommandHandlerThreadGate { get; }
   public IThreadGate MyRemoteTaggregateTeventHandlerThreadGate { get; }
   public IThreadGate MyRemotePublisherConsciousTeventHandlerThreadGate { get; }
   public IThreadGate MyLocalTaggregateTeventHandlerThreadGate { get; }
   public IThreadGate MyTransientTeventLocalHandlerThreadGate { get; }
   public IThreadGate MyTransientTeventRemoteHandlerThreadGate { get; }
   public IThreadGate MyTaggregateTeventBackendObserverThreadGate { get; }
   public IThreadGate MyTaggregateTeventRemoteObserverThreadGate { get; }
   public IThreadGate MyTransientTeventRemoteObserverThreadGate { get; }
   public IThreadGate TeventHandlerThreadGate { get; }
   public IThreadGate TueryHandlerThreadGate { get; }

   ///<summary>Every <see cref="IMyTransientTevent"/> the Remote endpoint's handler has received, in handling order — transient delivery promises in-order arrival within a connected session.</summary>
   protected ConcurrentQueue<IMyTransientTevent> RemotelyReceivedTransientTevents { get; } = new();

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
         TommandHandlerWithResultThreadGate = IThreadGate.NewOpen(_timeout, nameof(TommandHandlerWithResultThreadGate)),
         MyCreateTaggregateTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyCreateTaggregateTommandHandlerThreadGate)),
         MyUpdateTaggregateTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyUpdateTaggregateTommandHandlerThreadGate)),
         MyRemoteTaggregateTeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyRemoteTaggregateTeventHandlerThreadGate)),
         MyRemotePublisherConsciousTeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyRemotePublisherConsciousTeventHandlerThreadGate)),
         MyLocalTaggregateTeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyLocalTaggregateTeventHandlerThreadGate)),
         MyTransientTeventLocalHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyTransientTeventLocalHandlerThreadGate)),
         MyTransientTeventRemoteHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyTransientTeventRemoteHandlerThreadGate)),
         MyTaggregateTeventBackendObserverThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyTaggregateTeventBackendObserverThreadGate)),
         MyTaggregateTeventRemoteObserverThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyTaggregateTeventRemoteObserverThreadGate)),
         MyTransientTeventRemoteObserverThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyTransientTeventRemoteObserverThreadGate)),
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

   void InitializeHost()
   {
      _rootContainer ??= CreateRootBuilder().Build();
      Host = TestingEndpointHost.Create(_rootContainer, new ExactlyOnceTessagingTestingEndpointHostFeature(), new DistributedTypermediaTestingEndpointHostFeature());

      BackendEndPoint = Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
         builder =>
         {
            builder.TypeMapper.RegisterCommonTestTypeMappings();

            builder.RegisterTeventStore()
                   .HandleTaggregate<MyTaggregate, IMyTaggregateTevent>();

            builder.RegisterTessagingHandlers
                   .ForTommand((MyExactlyOnceTommand _) => MyExactlyOnceTommandHandlerThreadGate.AwaitPassThrough())
                   .ForTevent((IMyExactlyOnceTevent _) => TeventHandlerThreadGate.AwaitPassThrough())
                   .ForTevent((IMyTaggregateTevent _) => MyLocalTaggregateTeventHandlerThreadGate.AwaitPassThrough())
                   .ForTevent((IMyTransientTevent _) => MyTransientTeventLocalHandlerThreadGate.AwaitPassThrough());

            //Observation - the transaction-ignoring subscription kind: fires at publish time for the Backend's own locally published tevents.
            builder.RegisterTransactionIgnoringTeventHandlers
                   .ForTevent((IMyTaggregateTevent _) => MyTaggregateTeventBackendObserverThreadGate.AwaitPassThrough());

            builder.RegisterTypermediaHandlers
                   .ForTommand((MyCreateTaggregateTommand tommand, IInProcessTypermediaNavigator navigator) =>
                    {
                       MyCreateTaggregateTommandHandlerThreadGate.AwaitPassThrough();
                       MyTaggregate.Create(tommand.TaggregateId, navigator);
                    })
                   .ForTommand((MyUpdateTaggregateTommand tommand, IInProcessTypermediaNavigator navigator) =>
                    {
                       MyUpdateTaggregateTommandHandlerThreadGate.AwaitPassThrough();
                       navigator.Execute(new TeventStoreApi().Tueries.GetForUpdate<MyTaggregate>(tommand.TaggregateId)).Update();
                    })
                   .ForTuery((MyTuery _) =>
                    {
                       TueryHandlerThreadGate.AwaitPassThrough();
                       return new MyTueryResult();
                    })
                   .ForTommandWithResult((MyAtMostOnceTypermediaTommandWithResult _) =>
                    {
                       TommandHandlerWithResultThreadGate.AwaitPassThrough();
                       return new MyTommandResult();
                    });
         });

      RemoteEndpoint = Host.RegisterEndpoint("Remote",
                                             new EndpointId(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B")),
                                             builder =>
                                             {
                                                builder.TypeMapper.RegisterCommonTestTypeMappings();

                                                builder.RegisterTessagingHandlers
                                                       .ForTevent((IMyTaggregateTevent _) => MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassThrough())
                                                       //Publisher-conscious subscription: subscribing to the taggregate's wrapper type receives the wrapped tevent as MyTaggregate published it.
                                                       .ForTevent((IMyTaggregateTevent<IMyTaggregateTevent> _) => MyRemotePublisherConsciousTeventHandlerThreadGate.AwaitPassThrough())
                                                       .ForTevent((IMyTransientTevent tevent) =>
                                                        {
                                                           RemotelyReceivedTransientTevents.Enqueue(tevent);
                                                           MyTransientTeventRemoteHandlerThreadGate.AwaitPassThrough();
                                                        });

                                                //Observation - the transaction-ignoring subscription kind: fires on arrival, before and outside the transactional handling above.
                                                builder.RegisterTransactionIgnoringTeventHandlers
                                                       .ForTevent((IMyTaggregateTevent _) => MyTaggregateTeventRemoteObserverThreadGate.AwaitPassThrough())
                                                       .ForTevent((IMyTransientTevent _) => MyTransientTeventRemoteObserverThreadGate.AwaitPassThrough());
                                             });
   }

   protected async Task StartHostAsync()
   {
      InitializeHost();
      await Host.StartAsync();
      Client = await TypermediaTestClient.ConnectTo(BackendEndPoint.TypermediaAddress!, mapper => mapper.RegisterCommonTestTypeMappings());
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());

   protected void PublishTransientTeventOnTheBackendInATransaction(int sequenceNumber) =>
      BackendEndPoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteTransactionInIsolatedScope(scope =>
         scope.Resolve<ITeventPublisher>().Publish(new MyTransientTevent { SequenceNumber = sequenceNumber }));
}
