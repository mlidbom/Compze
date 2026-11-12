using Compze.DependencyInjection;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Must;
using Compze.Tessaging.TessageBus;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Parallelism_policies : EndpointHostTestBase
{
   [PCT] public async Task Five_tuery_handlers_can_execute_in_parallel_when_using_TueryAsync()
   {
      TueryHandlerThreadGate.Close();

      var tasks = Task.WhenAll(1.Through(5)
                                .Select(_ => Navigator.GetAsync(new MyTuery())));

      await TueryHandlerThreadGate.AwaitQueueLengthEqualToAsync(5);
      OpenGates();
      await tasks;
   }

   [PCT] public async Task Five_tuery_handlers_can_execute_in_parallel_when_using_Tuery()
   {
      TueryHandlerThreadGate.Close();
      var tasks = 1.Through(5).Select(_ => TaskCE.RunOnDedicatedThread(() => Navigator.Get(new MyTuery()))).ToList();

      await TueryHandlerThreadGate.AwaitQueueLengthEqualToAsync(5);
      TueryHandlerThreadGate.Open();
      await Task.WhenAll(tasks);
   }

   [PCT] public async Task Two_tevent_handlers_cannot_execute_in_parallel()
   {
      MyRemoteTaggregateTeventHandlerThreadGate.Close();
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());

      await MyRemoteTaggregateTeventHandlerThreadGate.AwaitQueueLengthEqualToAsync(1);
      (await MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitQueueLengthEqualToAsync(2, WaitTimeout.Milliseconds(100))).Must().Be(false);
   }

   [PCT] public async Task Two_exactly_once_tommand_handlers_cannot_execute_in_parallel()
   {
      CloseGates();

      await RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommand());
      await RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommand());

      await MyExactlyOnceTommandHandlerThreadGate.AwaitQueueLengthEqualToAsync(1);
      (await MyExactlyOnceTommandHandlerThreadGate.TryAwaitQueueLengthEqualToAsync(2, timeout: WaitTimeout.Milliseconds(100))).Must().Be(false);
   }
}
