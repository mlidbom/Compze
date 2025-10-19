using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.Functional;
using Compze.Utilities.Logging;
using Compze.Utilities.Threading.TasksCE;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Compze.Tests.Integration.Tessaging;

public class TaskRunnerExceptionHandlingTests : UniversalTestBase, IAsyncLifetime
{
#pragma warning disable CA2213 // Disposable fields should be disposed
    readonly ITestingEndpointHost _host;
#pragma warning restore CA2213 // Disposable fields should be disposed
    readonly ITaskRunner _taskRunner;

   public TaskRunnerExceptionHandlingTests()
   {
      _host = TestingEndpointHost.Create(TestingContainerFactory.CreateWithRegisteredServiceLocator);
      var endpoint = _host.RegisterEndpoint(
         "endpoint",
         new EndpointId(Guid.Parse("A1B2C3D4-E5F6-4748-9ABC-DEF012345678")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredSqlLayer();
         });

      _taskRunner = endpoint.ServiceLocator.Resolve<ITaskRunner>();
   }

   public async Task InitializeAsync() => await _host.StartAsync();

   public async Task DisposeAsync() => await Task.CompletedTask;

   [PCT]
   public async Task Should_throw_aggregate_exception_on_dispose_when_background_task_throws()
   {
      await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
      {
         var gate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

         _taskRunner.Run("test-task", () => gate.AwaitPassThrough().then(() => throw new InvalidOperationException("exception1")));

         gate.AwaitPassedThroughCountEqualTo(1);


         var disposeAction = async () => await _host.DisposeAsync().caf();
         var aggregateException = await disposeAction.Should().ThrowAsync<AggregateException>();

         var flattened = aggregateException.Which.Flatten();
         flattened.InnerExceptions.Should().Contain(e => e is InvalidOperationException && e.Message == "exception1");
      });
   }

   [PCT]
   public async Task Should_not_throw_on_dispose_when_no_exceptions_occurred()
   {
      var gate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

      _taskRunner.Run("test-task", gate.AwaitPassThrough);

      gate.AwaitPassedThroughCountEqualTo(1);

      var disposeAction = async () => await _host.DisposeAsync();
      await disposeAction.Should().NotThrowAsync();
   }

   [PCT]
   public async Task Should_collect_multiple_exceptions_from_multiple_background_tasks()
   {
      await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
      {
         var gate = ThreadGate.CreateOpenWithTimeout(20.Seconds());

         _taskRunner.Run("test-task-1", () => gate.AwaitPassThrough().then(() => throw new InvalidOperationException("exception1")));
         _taskRunner.Run("test-task-2", () => gate.AwaitPassThrough().then(() => throw new ArgumentException("exception2")));
         _taskRunner.Run("test-task-3", () => gate.AwaitPassThrough().then(() => throw new NotSupportedException("exception3")));

         gate.AwaitPassedThroughCountEqualTo(3);

         var disposeAction = async () => await _host.DisposeAsync().caf();
         var aggregateException = await disposeAction.Should().ThrowAsync<AggregateException>();

         var flattened = aggregateException.Which.Flatten();
         flattened.InnerExceptions.Should().HaveCount(3);
         flattened.InnerExceptions.Should().Contain(e => e is InvalidOperationException && e.Message == "exception1");
         flattened.InnerExceptions.Should().Contain(e => e is ArgumentException && e.Message == "exception2");
         flattened.InnerExceptions.Should().Contain(e => e is NotSupportedException && e.Message == "exception3");
      });
   }
}
