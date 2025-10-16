using System;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Time;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification;

public class When_scheduling_commands_to_be_sent_in_the_future : UniversalTestBase, IAsyncLifetime
{
   IUtcTimeTimeSource _timeSource = DateTimeNowTimeSource.Instance;
   readonly IThreadGate _receivedCommandGate;
   readonly ITestingEndpointHost _host;
   readonly IEndpoint _endpoint;

   public When_scheduling_commands_to_be_sent_in_the_future()
   {
      _host = TestingEndpointHost.Create(TestingContainerFactory.CreateWithRegisteredServiceLocator);
      _receivedCommandGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
      _endpoint = _host.RegisterEndpoint(
         "endpoint",
         new EndpointId(Guid.Parse("17ED9DF9-33A8-4DF8-B6EC-6ED97AB2030B")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredSqlLayer();
            builder.RegisterHandlers.ForCommand<ScheduledCommand>(_ => _receivedCommandGate.AwaitPassThrough());
         });
   }

   public async Task InitializeAsync()
   {
      await _host.StartAsync();

      var serviceLocator = _endpoint.ServiceLocator;
      _timeSource = serviceLocator.Resolve<IUtcTimeTimeSource>();
   }

   public async Task DisposeAsync() => await _host.DisposeAsync();

   [PCT]  public void Messages_whose_due_time_has_passed_are_delivered()
   {
      var now = _timeSource.UtcNow;
      var inOneHour = new ScheduledCommand();

      _endpoint.ExecuteServerRequestInTransaction(session => session.ScheduleSend(now + .2.Seconds(), inOneHour));

      _receivedCommandGate.AwaitPassedThroughCountEqualTo(1, timeout: 2.Seconds());
   }

   [PCT]  public void Messages_whose_due_time_have_not_passed_are_not_delivered()
   {
      var now = _timeSource.UtcNow;
      var inOneHour = new ScheduledCommand();
      _endpoint.ExecuteServerRequestInTransaction(session => session.ScheduleSend(now + 2.Seconds(), inOneHour));

      _receivedCommandGate.TryAwaitPassedThroughCountEqualTo(1, timeout: .5.Seconds())
                          .Should().Be(false);
   }

   internal class ScheduledCommand : MessageTypes.Remotable.ExactlyOnce.Command;
}
