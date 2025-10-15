using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Sql.EventStore;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Tessaging.Typermedia.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.Testing;
using FluentAssertions.Extensions;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public abstract partial class Fixture()
{
   static readonly TimeSpan _timeout = 10.Seconds();
   public ITestingEndpointHost Host = null!;
   public IThreadGate CommandHandlerThreadGate = null!;
   public IThreadGate CommandHandlerWithResultThreadGate = null!;
   public IThreadGate MyCreateAggregateCommandHandlerThreadGate = null!;
   public IThreadGate MyUpdateAggregateCommandHandlerThreadGate = null!;
   public IThreadGate MyRemoteAggregateEventHandlerThreadGate = null!;
   public IThreadGate MyLocalAggregateEventHandlerThreadGate = null!;
   public IThreadGate EventHandlerThreadGate = null!;
   public IThreadGate QueryHandlerThreadGate = null!;

   public IReadOnlyList<IThreadGate> AllGates = [];

   public IEndpoint BackendEndPoint { get; private set; } = null!;
   protected IEndpoint ClientEndpoint { get; set; } = null!;
   protected IEndpoint RemoteEndpoint { get; set; } = null!;

   public virtual async Task SetupAsync()
   {
      InitializeHost();
      await StartHostAsync();
   }

   protected void InitializeHost()
   {
      Host = TestingEndpointHost.Create(TestingContainerFactory.Create);

      BackendEndPoint = Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredSqlLayer();

            builder.RegisterEventStore()
                   .HandleAggregate<MyAggregate, MyAggregateEvent.IRoot>();

            builder.RegisterHandlers
                   .ForCommand((MyExactlyOnceCommand _) => CommandHandlerThreadGate.AwaitPassThrough())
                   .ForCommand((MyCreateAggregateCommand command, IInProcessHypermediaNavigator navigator) =>
                    {
                       MyCreateAggregateCommandHandlerThreadGate.AwaitPassThrough();
                       MyAggregate.Create(command.AggregateId, navigator);
                    })
                   .ForCommand((MyUpdateAggregateCommand command, IInProcessHypermediaNavigator navigator) =>
                    {
                       MyUpdateAggregateCommandHandlerThreadGate.AwaitPassThrough();
                       navigator.Execute(new EventStoreApi().Queries.GetForUpdate<MyAggregate>(command.AggregateId)).Update();
                    })
                   .ForEvent((IMyExactlyOnceEvent _) => EventHandlerThreadGate.AwaitPassThrough())
                   .ForEvent((MyAggregateEvent.IRoot _) => MyLocalAggregateEventHandlerThreadGate.AwaitPassThrough())
                   .ForQuery((MyQuery _) =>
                    {
                       QueryHandlerThreadGate.AwaitPassThrough();
                       return new MyQueryResult();
                    })
                   .ForCommandWithResult((MyAtMostOnceCommandWithResult _) =>
                    {
                       CommandHandlerWithResultThreadGate.AwaitPassThrough();
                       return new MyCommandResult();
                    });
         });

      RemoteEndpoint = Host.RegisterEndpoint("Remote",
                                             new EndpointId(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B")),
                                             builder =>
                                             {
                                                builder.Container.Register()
                                                       .AspNetCoreTransport()
                                                       .CurrentTestsConfiguredSqlLayer();
                                                builder.RegisterHandlers.ForEvent((MyAggregateEvent.IRoot _) => MyRemoteAggregateEventHandlerThreadGate.AwaitPassThrough());
                                             });

      ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();
   }

   protected async Task StartHostAsync()
   {
      await Host.StartAsync();
      AllGates =
      [
         CommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(CommandHandlerThreadGate)),
         CommandHandlerWithResultThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(CommandHandlerWithResultThreadGate)),
         MyCreateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyCreateAggregateCommandHandlerThreadGate)),
         MyUpdateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyUpdateAggregateCommandHandlerThreadGate)),
         MyRemoteAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyRemoteAggregateEventHandlerThreadGate)),
         MyLocalAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyLocalAggregateEventHandlerThreadGate)),
         EventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(EventHandlerThreadGate)),
         QueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(QueryHandlerThreadGate))
      ];
   }

   public virtual async Task TearDownAsync()
   {
      OpenGates();
      await Host.DisposeAsync();
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());
}
