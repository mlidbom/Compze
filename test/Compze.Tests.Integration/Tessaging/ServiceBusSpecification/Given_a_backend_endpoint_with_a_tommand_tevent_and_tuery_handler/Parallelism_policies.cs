using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Must;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Parallelism_policies : EndpointHostTestBase
{
   [PCT] public async Task Five_tuery_handlers_can_execute_in_parallel_when_using_TueryAsync()
   {
      TueryHandlerThreadGate.Close();

      var tasks = Task.WhenAll(1.Through(5)
                                .Select(_ => Client.ExecuteRequestAsync(session =>
                                                                                          session.GetAsync(new MyTuery()))));

      TueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
      OpenGates();
      await tasks;
   }

   [PCT] public async Task Five_tuery_handlers_can_execute_in_parallel_when_using_Tuery()
   {
      TueryHandlerThreadGate.Close();
      var tasks = 1.Through(5).Select(_ => TaskCE.Run(() => Client.ExecuteRequest(session => session.Get(new MyTuery())))).ToList();

      TueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
      TueryHandlerThreadGate.Open();
      await Task.WhenAll(tasks);
   }

   [PCT] public void Two_tevent_handlers_cannot_execute_in_parallel()
   {
      MyRemoteTaggregateTeventHandlerThreadGate.Close();
      Client.ExecuteRequest(session => session.Post(MyCreateTaggregateTommand.Create()));
      Client.ExecuteRequest(session => session.Post(MyCreateTaggregateTommand.Create()));

      MyRemoteTaggregateTeventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                             .TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(100)).Must().Be(false);
   }

   [PCT] public void Two_exactly_once_tommand_handlers_cannot_execute_in_parallel()
   {
      CloseGates();

      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceTommand()));
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceTommand()));

      MyExactlyOnceTommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                              .TryAwaitQueueLengthEqualTo(2, timeout: WaitTimeout.Milliseconds(100)).Must().Be(false);
   }

   [PCT] public async Task Two_AtMostOnce_tommand_handlers_from_the_same_session_cannot_execute_in_parallel()
   {
      CloseGates();

      var commandsCompleted = Client.ExecuteRequestAsync(async session =>
         await Task.WhenAll(
            session.PostAsync(MyCreateTaggregateTommand.Create()),
            session.PostAsync(MyCreateTaggregateTommand.Create())));

      MyCreateTaggregateTommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                               .TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(100)).Must().Be(false);

      OpenGates();
      await commandsCompleted;
   }
}
