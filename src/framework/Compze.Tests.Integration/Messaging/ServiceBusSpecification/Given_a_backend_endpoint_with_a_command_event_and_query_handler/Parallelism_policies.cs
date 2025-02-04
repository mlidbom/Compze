﻿using System.Linq;
using System.Threading.Tasks;
using Compze.Messaging.Buses;
using Compze.SystemCE.LinqCE;
using Compze.Testing.Threading;
using Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Compze.Tests.Integration.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Parallelism_policies(string pluggableComponentsCombination) : Fixture(pluggableComponentsCombination)
{
   [Test] public async Task Five_query_handlers_can_execute_in_parallel_when_using_QueryAsync()
   {
      QueryHandlerThreadGate.Close();

      var tasks = Task.WhenAll(1.Through(5)
                                .Select(_ => ClientEndpoint.ExecuteClientRequestAsync(session =>
                                                                                          session.GetAsync(new MyQuery()))));

      QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
      OpenGates();
      await tasks;
   }

   [Test] public async Task Five_query_handlers_can_execute_in_parallel_when_using_Query()
   {
      QueryHandlerThreadGate.Close();
      var tasks = 1.Through(5).Select(_ => Task.Run(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery())))).ToList();

      QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
      QueryHandlerThreadGate.Open();
      await Task.WhenAll(tasks);
   }

   [Test] public void Two_event_handlers_cannot_execute_in_parallel()
   {
      MyRemoteAggregateEventHandlerThreadGate.Close();
      ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));
      ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));

      MyRemoteAggregateEventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                             .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
   }

   [Test] public void Two_exactly_once_command_handlers_cannot_execute_in_parallel()
   {
      CloseGates();

      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

      CommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                              .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
   }

   [Test] public void Two_AtMostOnce_command_handlers_from_the_same_session_cannot_execute_in_parallel()
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
