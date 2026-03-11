using Compze.Tessaging.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Hosting;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Teventive.TeventStore.Typermedia;
using Compze.Tests.Infrastructure;
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
   static readonly WaitTimeout _timeout = WaitTimeout.Seconds(10);
   protected ITestingEndpointHost Host { get; private set; } = null!;
   public IThreadGate MyExactlyOnceTommandHandlerThreadGate { get; }
   public IThreadGate TommandHandlerWithResultThreadGate { get; }
   public IThreadGate MyCreateTaggregateTommandHandlerThreadGate { get; }
   public IThreadGate MyUpdateTaggregateTommandHandlerThreadGate { get; }
   public IThreadGate MyRemoteTaggregateTeventHandlerThreadGate { get; }
   public IThreadGate MyLocalTaggregateTeventHandlerThreadGate { get; }
   public IThreadGate TeventHandlerThreadGate { get; }
   public IThreadGate TueryHandlerThreadGate { get; }

   IReadOnlyList<IThreadGate> AllGates  { get; }

   protected IEndpoint BackendEndPoint { get; private set; } = null!;
   TestClient Client { get; set; } = null!;
   protected IRemoteTypermediaNavigator Navigator => Client.Navigator;
   protected IEndpoint RemoteEndpoint { get; private set; } = null!;

   readonly IDependencyInjectionContainer _rootContainer;

   protected EndpointHostTestBase()
   {
#pragma warning disable CA2000 // We are passing this disposable into a constructor of an object we don't own
      _rootContainer = TestEnv.DIContainer.CreateWithServiceLocator()
                              ._mutate(it => it.Register().CurrentTestsDbPoolIfNotCloneContainer());
#pragma warning restore CA2000 // We are passing this disposable into a constructor of an object we don't own

      AllGates =
      [
         MyExactlyOnceTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyExactlyOnceTommandHandlerThreadGate)),
         TommandHandlerWithResultThreadGate = IThreadGate.NewOpen(_timeout, nameof(TommandHandlerWithResultThreadGate)),
         MyCreateTaggregateTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyCreateTaggregateTommandHandlerThreadGate)),
         MyUpdateTaggregateTommandHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyUpdateTaggregateTommandHandlerThreadGate)),
         MyRemoteTaggregateTeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyRemoteTaggregateTeventHandlerThreadGate)),
         MyLocalTaggregateTeventHandlerThreadGate = IThreadGate.NewOpen(_timeout, nameof(MyLocalTaggregateTeventHandlerThreadGate)),
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
      _rootContainer.Dispose();
   }

   void InitializeHost()
   {
      Host = TestingEndpointHost.Create(_rootContainer);

      BackendEndPoint = Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
         builder =>
         {
            builder.RegisterTeventStore()
                   .HandleTaggregate<MyTaggregate, IMyTaggregateTevent>();

            builder.RegisterTessagingHandlers
                   .ForTommand((MyExactlyOnceTommand _) => MyExactlyOnceTommandHandlerThreadGate.AwaitPassThrough())
                   .ForTevent((IMyExactlyOnceTevent _) => TeventHandlerThreadGate.AwaitPassThrough())
                   .ForTevent((IMyTaggregateTevent _) => MyLocalTaggregateTeventHandlerThreadGate.AwaitPassThrough());

            builder.RegisterTypermediaHandlers()
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
                                                builder.RegisterTessagingHandlers.ForTevent((IMyTaggregateTevent _) => MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassThrough());
                                             });
   }

   protected async Task StartHostAsync()
   {
      InitializeHost();
      await Host.StartAsync();
      Client = await TestClient.ConnectTo(BackendEndPoint.TypermediaAddress!);
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());
}
