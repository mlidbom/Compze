using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Must;
using static Compze.Must.MustActions;

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
      var endpoint = _host.RegisterEndpoint("endpoint", new EndpointId(Guid.Parse("A1B2C3D4-E5F6-4748-9ABC-DEF012345678")), builder => builder.TypeMapper.RegisterIntegrationTestTypeMappings());

      _taskRunner = endpoint.ServiceLocator.Resolve<ITaskRunner>();
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();
   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT]
   public async Task Should_throw_taggregate_exception_on_dispose_when_background_task_throws()
   {
      await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
      {
         var gate = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "gate");

         _taskRunner.Run("test-task", () => gate.AwaitPassThrough().__(() => throw new InvalidOperationException("exception1")));

         gate.AwaitPassedThroughCountEqualTo(1);

         var disposeAction = async () => await _host.DisposeAsync().caf();
         var taggregateException = await disposeAction.Must().ThrowAsync<AggregateException>();

         var flattened = taggregateException.Which.Flatten();
         flattened.InnerExceptions.Must().SatisfyInternal(it => it.Any(e => e.InnerException is InvalidOperationException && e.InnerException.Message == "exception1"));
      });
   }

   [PCT]
   public async Task Should_collect_multiple_exceptions_from_multiple_background_tasks()
   {
      await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
      {
         var gate = IThreadGate.NewOpen(WaitTimeout.Seconds(20), "gate");

         var exception1 = new InvalidOperationException("exception1");
         var exception2 = new ArgumentException("exception2");
         var exception3 = new NotSupportedException("exception3");

         _taskRunner.Run("test-task-1", () => gate.AwaitPassThrough().__(() => throw exception1));
         _taskRunner.Run("test-task-2", () => gate.AwaitPassThrough().__(() => throw exception2));
         _taskRunner.Run("test-task-3", () => gate.AwaitPassThrough().__(() => throw exception3));

         gate.AwaitPassedThroughCountEqualTo(3);

         (await InvokingAsync(async () => await _host.DisposeAsync()).Must().ThrowAsync<AggregateException>())
           .Which
           .Flatten()
           .InnerExceptions
           .Must()
           .HaveCount(3)
           .Satisfy(it => it.Any(ex => ex.InnerException == exception1))
           .Satisfy(it => it.Any(ex => ex.InnerException == exception2))
           .Satisfy(it => it.Any(ex => ex.InnerException == exception3));
      });
   }
}
