using Compze.DependencyInjection;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Must;
using Compze.Tessaging.Abstractions.TessageBus;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Parallelism_policies : EndpointHostTestBase
{
   [PCT] public async Task Five_tuery_handlers_can_execute_in_parallel_when_using_TueryAsync()
   {
      TueryHandlerThreadGate.Close();

      var tasks = Task.WhenAll(1.Through(5)
                                .Select(_ => Navigator.GetAsync(new MyTuery())));

      TueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
      OpenGates();
      await tasks;
   }

   [PCT] public async Task Five_tuery_handlers_can_execute_in_parallel_when_using_Tuery()
   {
      TueryHandlerThreadGate.Close();
      var tasks = 1.Through(5).Select(_ => TaskCE.Run(() => Navigator.Get(new MyTuery()))).ToList();

      TueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
      TueryHandlerThreadGate.Open();
      await Task.WhenAll(tasks);
   }

   [PCT] public void Two_tevent_handlers_cannot_execute_in_parallel()
   {
      MyRemoteTaggregateTeventHandlerThreadGate.Close();
      Navigator.Post(MyCreateTaggregateTommand.Create());
      Navigator.Post(MyCreateTaggregateTommand.Create());

      MyRemoteTaggregateTeventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                             .TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(100)).Must().Be(false);
   }

   [PCT] public async Task Two_exactly_once_tommand_handlers_cannot_execute_in_parallel()
   {
      CloseGates();

      await RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommand());
      await RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommand());

      MyExactlyOnceTommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                              .TryAwaitQueueLengthEqualTo(2, timeout: WaitTimeout.Milliseconds(100)).Must().Be(false);
   }
}
