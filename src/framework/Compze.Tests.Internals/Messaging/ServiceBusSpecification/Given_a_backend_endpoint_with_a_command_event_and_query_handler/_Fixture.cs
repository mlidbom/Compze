using Compze.Functional;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Review OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public abstract class Fixture(string pluggableComponentsCombination) : FixtureBase(pluggableComponentsCombination)
{
   protected override void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
   {
      base.RegisterHandlers(registrar);

      registrar.ForCommand((MyCreateAggregateCommand command, ILocalHypermediaNavigator navigator) => MyCreateAggregateCommandHandlerThreadGate.AwaitPassThrough().then(() => MyAggregate.Create(command.AggregateId, navigator)));
   }
}
