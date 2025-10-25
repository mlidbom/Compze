using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.Testing;
using FluentAssertions.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.TyperMediaApi.EventStore;
using Compze.Tests.Infrastructure;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_event_and_tuery_handler;

public abstract class EndpointHostTestBase : UniversalTestBase
{
   static readonly TimeSpan _timeout = 10.Seconds();
   public ITestingEndpointHost Host = null!;
   public readonly IThreadGate MyExactlyOnceTommandHandlerThreadGate;
   public readonly IThreadGate TommandHandlerWithResultThreadGate;
   public readonly IThreadGate MyCreateAggregateTommandHandlerThreadGate;
   public readonly IThreadGate MyUpdateAggregateTommandHandlerThreadGate;
   public readonly IThreadGate MyRemoteAggregateEventHandlerThreadGate;
   public readonly IThreadGate MyLocalAggregateEventHandlerThreadGate;
   public readonly IThreadGate EventHandlerThreadGate;
   public readonly IThreadGate TueryHandlerThreadGate;

   public readonly IReadOnlyList<IThreadGate> AllGates;

   public IEndpoint BackendEndPoint { get; private set; } = null!;
   protected IEndpoint ClientEndpoint { get; private set; } = null!;
   protected IEndpoint RemoteEndpoint { get; private set; } = null!;

   readonly IDependencyInjectionContainer _rootContainer;

   protected EndpointHostTestBase()
   {
      _rootContainer = TestEnv.DIContainer.Create();
      _rootContainer.Register()
                    .CurrentTestsDbPoolIfNotAlreadyRegistered();

      AllGates =
      [
         MyExactlyOnceTommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyExactlyOnceTommandHandlerThreadGate)),
         TommandHandlerWithResultThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(TommandHandlerWithResultThreadGate)),
         MyCreateAggregateTommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyCreateAggregateTommandHandlerThreadGate)),
         MyUpdateAggregateTommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyUpdateAggregateTommandHandlerThreadGate)),
         MyRemoteAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyRemoteAggregateEventHandlerThreadGate)),
         MyLocalAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyLocalAggregateEventHandlerThreadGate)),
         EventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(EventHandlerThreadGate)),
         TueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(TueryHandlerThreadGate))
      ];
   }

   protected override async Task InitializeAsyncInternal() => await StartHostAsync();

   protected override async Task DisposeAsyncInternal()
   {
      OpenGates();
      await Host.DisposeAsync();
      _rootContainer.Dispose();
   }

   void InitializeHost()
   {
      IDependencyInjectionContainer CreateCloneContainerWithParentContainerKeepingTheDbPoolAliveAfterChildContainersAreDisposed()
      {
         var clone = _rootContainer.Clone();
         return clone;
      }

      Host = TestingEndpointHost.Create(x => CreateCloneContainerWithParentContainerKeepingTheDbPoolAliveAfterChildContainersAreDisposed());

      BackendEndPoint = Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredSqlLayer("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6");

            builder.RegisterEventStore()
                   .HandleAggregate<MyAggregate, MyAggregateEvent.IRoot>();

            builder.RegisterHandlers
                   .ForTommand((MyExactlyOnceTommand _) => MyExactlyOnceTommandHandlerThreadGate.AwaitPassThrough())
                   .ForTommand((MyCreateAggregateTommand tommand, IInProcessHypermediaNavigator navigator) =>
                    {
                       MyCreateAggregateTommandHandlerThreadGate.AwaitPassThrough();
                       MyAggregate.Create(tommand.AggregateId, navigator);
                    })
                   .ForTommand((MyUpdateAggregateTommand tommand, IInProcessHypermediaNavigator navigator) =>
                    {
                       MyUpdateAggregateTommandHandlerThreadGate.AwaitPassThrough();
                       navigator.Execute(new EventStoreApi().Queries.GetForUpdate<MyAggregate>(tommand.AggregateId)).Update();
                    })
                   .ForEvent((IMyExactlyOnceTevent _) => EventHandlerThreadGate.AwaitPassThrough())
                   .ForEvent((MyAggregateEvent.IRoot _) => MyLocalAggregateEventHandlerThreadGate.AwaitPassThrough())
                   .ForTuery((MyTuery _) =>
                    {
                       TueryHandlerThreadGate.AwaitPassThrough();
                       return new MyTueryResult();
                    })
                   .ForTommandWithResult((MyAtMostOnceTommandWithResult _) =>
                    {
                       TommandHandlerWithResultThreadGate.AwaitPassThrough();
                       return new MyTommandResult();
                    });
         });

      RemoteEndpoint = Host.RegisterEndpoint("Remote",
                                             new EndpointId(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B")),
                                             builder =>
                                             {
                                                builder.Container.Register()
                                                       .AspNetCoreTransport()
                                                       .CurrentTestsConfiguredSqlLayer("E72924D3-5279-44B5-B20D-D682E537672B");
                                                builder.RegisterHandlers.ForEvent((MyAggregateEvent.IRoot _) => MyRemoteAggregateEventHandlerThreadGate.AwaitPassThrough());
                                             });

      ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();
   }

   protected async Task StartHostAsync()
   {
      InitializeHost();
      await Host.StartAsync();
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());
}
