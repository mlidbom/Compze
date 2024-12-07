using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;
using Compze.Persistence.Common.DependencyInjection;
using Compze.SystemCE.LinqCE;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.Messaging.Buses;
using Compze.Testing.Persistence;
using Compze.Testing.Threading;
using FluentAssertions.Extensions;
using NUnit.Framework;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Review OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public abstract partial class Fixture(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   static readonly TimeSpan _timeout = 10.Seconds();
   internal ITestingEndpointHost Host;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   internal IThreadGate CommandHandlerThreadGate;
   internal IThreadGate CommandHandlerWithResultThreadGate;
   internal IThreadGate MyCreateAggregateCommandHandlerThreadGate;
   internal IThreadGate MyUpdateAggregateCommandHandlerThreadGate;
   internal IThreadGate MyRemoteAggregateEventHandlerThreadGate;
   internal IThreadGate MyLocalAggregateEventHandlerThreadGate;
   internal IThreadGate EventHandlerThreadGate;
   internal IThreadGate QueryHandlerThreadGate;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

   internal IReadOnlyList<IThreadGate> AllGates = [];

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
            builder.RegisterCurrentTestsConfiguredPersistenceLayer();
            builder.RegisterEventStore()
                   .HandleAggregate<MyAggregate, MyAggregateEvent.IRoot>();

            builder.RegisterHandlers
                   .ForCommand((MyExactlyOnceCommand _) => CommandHandlerThreadGate.AwaitPassThrough())
                   .ForCommand((MyCreateAggregateCommand command, ILocalHypermediaNavigator navigator) =>
                    {
                       MyCreateAggregateCommandHandlerThreadGate.AwaitPassThrough();
                       MyAggregate.Create(command.AggregateId, navigator);
                    })
                   .ForCommand((MyUpdateAggregateCommand command, ILocalHypermediaNavigator navigator) =>
                    {
                       MyUpdateAggregateCommandHandlerThreadGate.AwaitPassThrough();
                       navigator.Execute(new CompzeApi().EventStore.Queries.GetForUpdate<MyAggregate>(command.AggregateId)).Update();
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

            MapBackendEndpointTypes(builder);
         });

      RemoteEndpoint = Host.RegisterEndpoint("Remote",
                                             new EndpointId(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B")),
                                             builder =>
                                             {
                                                builder.RegisterCurrentTestsConfiguredPersistenceLayer();
                                                builder.RegisterHandlers.ForEvent((MyAggregateEvent.IRoot _) => MyRemoteAggregateEventHandlerThreadGate.AwaitPassThrough());
                                                MapBackendEndpointTypes(builder);
                                             });

      ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();

      await Host.StartAsync();
      AllGates = [
                    CommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(CommandHandlerThreadGate)),
                    CommandHandlerWithResultThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(CommandHandlerWithResultThreadGate)),
                    MyCreateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyCreateAggregateCommandHandlerThreadGate)),
                    MyUpdateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyUpdateAggregateCommandHandlerThreadGate)),
                    MyRemoteAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyRemoteAggregateEventHandlerThreadGate)),
                    MyLocalAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyLocalAggregateEventHandlerThreadGate)),
                    EventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(EventHandlerThreadGate)),
                    QueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(QueryHandlerThreadGate))
                 ];
      return;

      static void MapBackendEndpointTypes(IEndpointBuilder builder) =>
         builder.TypeMapper.Map<MyExactlyOnceCommand>("0ddefcaa-4d4d-48b2-9e1a-762c0b835275")
                .Map<MyAtMostOnceCommandWithResult>("24248d03-630b-4909-a6ea-e7fdaf82baa2")
                .Map<MyExactlyOnceEvent>("2fdde21f-c6d4-46a2-95e5-3429b820dfc3")
                .Map<IMyExactlyOnceEvent>("49ba71a4-5f4c-4930-9e01-62bc0551d8c8")
                .Map<MyQuery>("b9d62f22-514b-4e3c-9ac1-66940a7a8144")
                .Map<MyCreateAggregateCommand>("86bf04d8-8e6d-4e21-a95e-8af237f69f0f")
                .Map<MyUpdateAggregateCommand>("c4ce3662-d068-4ec1-9c02-8d8f08640414")
                .Map<MyAggregateEvent.IRoot>("8b19a261-b74b-4c05-91e3-d062dc879635")
                .Map<MyAggregate>("8b7df016-3763-4033-8240-f46fa836ebfb")
                .Map<MyAggregateEvent.Created>("41f96e37-657f-464a-a4d1-004eba4e8e7b")
                .Map<MyAggregateEvent.Implementation.Created>("0ea2f548-0d24-4bb0-a59a-820bc35f3935")
                .Map<MyAggregateEvent.Implementation.Root>("5a792961-3fbc-4d50-b06e-77fc35cb6edf")
                .Map<MyAggregateEvent.Implementation.Updated>("bead75b3-9ecf-4f6b-b8c6-973a02168256")
                .Map<MyAggregateEvent.Updated>("2a8b19f0-20df-480d-b120-71ed5151b174")
                .Map<MyCommandResult>("4b2f17d2-2997-4532-9296-689495ed6958")
                .Map<MyQueryResult>("9f3c69f0-0886-483c-a726-b79fb1c56120");
   }

   [TearDown] public virtual async Task TearDownAsync()
   {
      OpenGates();
      await Host.DisposeAsync();
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());
}
