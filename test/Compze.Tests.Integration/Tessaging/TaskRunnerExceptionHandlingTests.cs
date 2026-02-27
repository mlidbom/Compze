using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Functional;
using Compze.Utilities.Logging;
using System;
using Compze.Utilities.SystemCE;
using System.Linq;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.Testing;
using Compze.Utilities.Testing.Must;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging;

public class TaskRunnerExceptionHandlingTests : UniversalTestBase
{
#pragma warning disable CA2213 // Disposable fields should be disposed
   readonly ITestingEndpointHost _host;
#pragma warning restore CA2213 // Disposable fields should be disposed
   readonly ITaskRunner _taskRunner;

   public TaskRunnerExceptionHandlingTests()
   {
      _host = TestingEndpointHost.Create();
      var endpoint = _host.RegisterEndpoint(
         "endpoint",
         new EndpointId(Guid.Parse("A1B2C3D4-E5F6-4748-9ABC-DEF012345678")),
         builder => {});

      _taskRunner = endpoint.ServiceLocator.Resolve<ITaskRunner>();
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();
   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT]
   public async Task Should_throw_taggregate_exception_on_dispose_when_background_task_throws()
   {
      await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
      {
         var gate = ThreadGate.CreateOpenWithTimeout(WaitTimeout.Seconds(10));

         _taskRunner.Run("test-task", () => gate.AwaitPassThrough()._then(() => throw new InvalidOperationException("exception1")));

         gate.AwaitPassedThroughCountEqualTo(1);

         var disposeAction = async () => await _host.DisposeAsync().caf();
         var taggregateException = await disposeAction.Must().ThrowAsync<AggregateException>();

         var flattened = taggregateException.Which.Flatten();
         flattened.InnerExceptions.Must().SatisfyInternal(it => it.Any(e => e is InvalidOperationException && e.Message == "exception1"));
      });
   }

   [PCT]
   public async Task Should_collect_multiple_exceptions_from_multiple_background_tasks()
   {
      await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
      {
         var gate = ThreadGate.CreateOpenWithTimeout(WaitTimeout.Seconds(20));

         var _exception1 = new InvalidOperationException("exception1");
         var _exception2 = new ArgumentException("exception2");
         var _exception3 = new NotSupportedException("exception3");

         _taskRunner.Run("test-task-1", () => gate.AwaitPassThrough()._then(() => throw _exception1));
         _taskRunner.Run("test-task-2", () => gate.AwaitPassThrough()._then(() => throw _exception2));
         _taskRunner.Run("test-task-3", () => gate.AwaitPassThrough()._then(() => throw _exception3));

         gate.AwaitPassedThroughCountEqualTo(3);

         (await InvokingAsync(async () => await _host.DisposeAsync()).Must().ThrowAsync<AggregateException>())
           .Which
           .Flatten()
           .InnerExceptions
           .Must()
           .HaveCount(3)
           .Contain(_exception1)
           .Contain(_exception2)
           .Contain(_exception3);
      });
   }
}
