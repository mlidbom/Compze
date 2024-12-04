using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Functional;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;
using Compze.Persistence.Common.DependencyInjection;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.Messaging.Buses;
using Compze.Testing.Persistence;
using Compze.Testing.Threading;
using NUnit.Framework;

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public abstract class FixtureBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   [SetUp] public virtual async Task Setup()
   {
      Host = TestingEndpointHost.Create(TestingContainerFactory.Create);
      
            Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
         builder =>
         {
            BuildEndpoint(builder);
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

      await Host.StartAsync().CaF();
      AllGates = new List<IThreadGate>
                 {
                    (CommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(CommandHandlerThreadGate))),
                    (CommandHandlerWithResultThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(CommandHandlerWithResultThreadGate))),
                    (MyCreateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyCreateAggregateCommandHandlerThreadGate))),
                    (MyUpdateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyUpdateAggregateCommandHandlerThreadGate))),
                    (MyRemoteAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyRemoteAggregateEventHandlerThreadGate))),
                    (MyLocalAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(MyLocalAggregateEventHandlerThreadGate))),
                    (EventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(EventHandlerThreadGate))),
                    (QueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout, nameof(QueryHandlerThreadGate)))
                 };
   }

   protected virtual void BuildEndpoint(IEndpointBuilder builder)
   {
      builder.RegisterCurrentTestsConfiguredPersistenceLayer();
      builder.RegisterEventStore()
             .HandleAggregate<MyAggregate, MyAggregateEvent.IRoot>();
      
      RegisterHandlers(builder.RegisterHandlers);
   }

   protected virtual void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => 
      registrar.ForCommand((MyExactlyOnceCommand _) => CommandHandlerThreadGate.AwaitPassThrough())
               .ForCommand((MyUpdateAggregateCommand command, ILocalHypermediaNavigator navigator) => MyUpdateAggregateCommandHandlerThreadGate.AwaitPassThrough().then(() => navigator.Execute(new CompzeApi().EventStore.Queries.GetForUpdate<MyAggregate>(command.AggregateId)).Update()))
               .ForEvent((IMyExactlyOnceEvent _) => EventHandlerThreadGate.AwaitPassThrough())
               .ForEvent((MyAggregateEvent.IRoot _) => MyLocalAggregateEventHandlerThreadGate.AwaitPassThrough())
               .ForQuery((MyQuery _) => QueryHandlerThreadGate.AwaitPassThrough().then(new MyQueryResult()))
               .ForCommandWithResult((MyAtMostOnceCommandWithResult _) => CommandHandlerWithResultThreadGate.AwaitPassThrough().then(() => new MyCommandResult()));

   protected static void MapBackendEndpointTypes(IEndpointBuilder builder) =>
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

   internal ITestingEndpointHost Host;
   internal IThreadGate CommandHandlerThreadGate;
   internal IThreadGate CommandHandlerWithResultThreadGate;
   internal IThreadGate MyCreateAggregateCommandHandlerThreadGate;
   internal IThreadGate MyUpdateAggregateCommandHandlerThreadGate;
   internal IThreadGate MyRemoteAggregateEventHandlerThreadGate;
   internal IThreadGate MyLocalAggregateEventHandlerThreadGate;
   internal IThreadGate EventHandlerThreadGate;
   internal IThreadGate QueryHandlerThreadGate;
   internal IReadOnlyList<IThreadGate> AllGates = [];
   protected static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
   protected IEndpoint ClientEndpoint { get; set; }
   protected IEndpoint RemoteEndpoint { get; set; }
   protected IRemoteHypermediaNavigator RemoteNavigator => ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();

   [TearDown] public virtual async Task TearDownAsync()
   {
      OpenGates();
      await Host.DisposeAsync().CaF();
   }

   protected void CloseGates() => AllGates.ForEach(gate => gate.Close());
   protected void OpenGates() => AllGates.ForEach(gate => gate.Open());
}
