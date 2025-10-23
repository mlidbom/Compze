using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Threading.TasksCE;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Parallelism_policies : EndpointHostTestBase
{
   [PCT] public async Task Five_query_handlers_can_execute_in_parallel_when_using_QueryAsync()
   {
      QueryHandlerThreadGate.Close();

      var tasks = Task.WhenAll(1.Through(5)
                                .Select(_ => ClientEndpoint.ExecuteClientRequestAsync(session =>
                                                                                          session.GetAsync(new MyQuery()))));

      QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
      OpenGates();
      await tasks;
   }

   [PCT] public async Task Five_query_handlers_can_execute_in_parallel_when_using_Query()
   {
      QueryHandlerThreadGate.Close();
      var tasks = 1.Through(5).Select(_ => TaskCE.Run(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery())))).ToList();

      QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
      QueryHandlerThreadGate.Open();
      await Task.WhenAll(tasks);
   }

   [PCT] public void Two_event_handlers_cannot_execute_in_parallel()
   {
      MyRemoteAggregateEventHandlerThreadGate.Close();
      ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));
      ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));

      MyRemoteAggregateEventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                             .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
   }

   [PCT] public void Two_exactly_once_command_handlers_cannot_execute_in_parallel()
   {
      CloseGates();

      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

      MyExactlyOnceCommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                              .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
   }

   [PCT] public void Two_AtMostOnce_command_handlers_from_the_same_session_cannot_execute_in_parallel()
   {
      CloseGates();

      ClientEndpoint.ExecuteClientRequest(session =>
      {
         session.PostAsync(MyCreateAggregateCommand.Create());
         session.PostAsync(MyCreateAggregateCommand.Create());
      });

      MyCreateAggregateCommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                               .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
   }
}
