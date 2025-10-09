using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Persistence;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Persistence.EventStore;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Tessaging.Typermedia.Abstractions;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Threading;
using Compze.Utilities.SystemCE.LinqCE;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Review OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.NUnit.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public abstract class NUnitFixtureBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   static readonly TimeSpan _timeout = 10.Seconds();
   public ITestingEndpointHost Host;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   public IThreadGate CommandHandlerThreadGate;
   public IThreadGate CommandHandlerWithResultThreadGate;
   public IThreadGate MyCreateAggregateCommandHandlerThreadGate;
   public IThreadGate MyUpdateAggregateCommandHandlerThreadGate;
   public IThreadGate MyRemoteAggregateEventHandlerThreadGate;
   public IThreadGate MyLocalAggregateEventHandlerThreadGate;
   public IThreadGate EventHandlerThreadGate;
   public IThreadGate QueryHandlerThreadGate;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

   public IReadOnlyList<IThreadGate> AllGates = [];

   protected IEndpoint ClientEndpoint { get; set; }
   protected IEndpoint RemoteEndpoint { get; set; }

   [SetUp] public async Task Setup()
   {
      Host = TestingEndpointHost.Create(TestingContainerFactory.Create);

      Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredPersistenceLayer();

            builder.RegisterEventStore()
                   .HandleAggregate<Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyAggregate, Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyAggregateEvent.IRoot>();

            builder.RegisterHandlers
                   .ForCommand((Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyExactlyOnceCommand _) => CommandHandlerThreadGate.AwaitPassThrough())
                   .ForCommand((Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyCreateAggregateCommand command, IInProcessHypermediaNavigator navigator) =>
                    {
                       MyCreateAggregateCommandHandlerThreadGate.AwaitPassThrough();
                       Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyAggregate.Create(command.AggregateId, navigator);
                    })
                   .ForCommand((Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyUpdateAggregateCommand command, IInProcessHypermediaNavigator navigator) =>
                    {
                       MyUpdateAggregateCommandHandlerThreadGate.AwaitPassThrough();
                       navigator.Execute(new EventStoreApi().Queries.GetForUpdate<Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyAggregate>(command.AggregateId)).Update();
                    })
                   .ForEvent((Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.IMyExactlyOnceEvent _) => EventHandlerThreadGate.AwaitPassThrough())
                   .ForEvent((Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyAggregateEvent.IRoot _) => MyLocalAggregateEventHandlerThreadGate.AwaitPassThrough())
                   .ForQuery((Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyQuery _) =>
                    {
                       QueryHandlerThreadGate.AwaitPassThrough();
                       return new Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyQueryResult();
                    })
                   .ForCommandWithResult((Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyAtMostOnceCommandWithResult _) =>
                    {
                       CommandHandlerWithResultThreadGate.AwaitPassThrough();
                       return new Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyCommandResult();
                    });
         });

      RemoteEndpoint = Host.RegisterEndpoint("Remote",
                                             new EndpointId(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B")),
                                             builder =>
                                             {
                                                builder.Container.Register()
                                                       .AspNetCoreTransport()
                                                       .CurrentTestsConfiguredPersistenceLayer();
                                                builder.RegisterHandlers.ForEvent((Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture.MyAggregateEvent.IRoot _) => MyRemoteAggregateEventHandlerThreadGate.AwaitPassThrough());
                                             });

      ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();

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

   [TearDown] public virtual async Task TearDownAsync()
   {
      OpenGates();
      await Host.DisposeAsync();
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());
}
