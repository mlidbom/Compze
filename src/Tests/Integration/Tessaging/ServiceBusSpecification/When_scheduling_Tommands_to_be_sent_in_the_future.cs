using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Time.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification;

public class When_scheduling_tommands_to_be_sent_in_the_future : UniversalTestBase, IAsyncLifetime
{
   IUtcTimeTimeSource _timeSource = DateTimeNowTimeSource.Instance;
   readonly IThreadGate _receivedTommandGate;
   readonly ITestingEndpointHost _host;
   readonly IEndpoint _endpoint;

   public When_scheduling_tommands_to_be_sent_in_the_future()
   {
      _host = TestingEndpointHost.Create(registrar => TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents());
      _receivedTommandGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
      _endpoint = _host.RegisterEndpoint(
         "endpoint",
         new EndpointId(Guid.Parse("17ED9DF9-33A8-4DF8-B6EC-6ED97AB2030B")),
         builder =>
         {
            builder.RegisterHandlers.ForTommand<ScheduledTommand>(_ => _receivedTommandGate.AwaitPassThrough());
         });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();

      var serviceLocator = _endpoint.ServiceLocator;
      _timeSource = serviceLocator.Resolve<IUtcTimeTimeSource>();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT]  public void Tessages_whose_due_time_has_passed_are_delivered()
   {
      var now = _timeSource.UtcNow;
      var inOneHour = new ScheduledTommand();

      _endpoint.ExecuteServerRequestInTransaction(session => session.ScheduleSend(now + .2.Seconds(), inOneHour));

      _receivedTommandGate.AwaitPassedThroughCountEqualTo(1, timeout: 2.Seconds());
   }

   [PCT]  public void Tessages_whose_due_time_have_not_passed_are_not_delivered()
   {
      var now = _timeSource.UtcNow;
      var inOneHour = new ScheduledTommand();
      _endpoint.ExecuteServerRequestInTransaction(session => session.ScheduleSend(now + 2.Seconds(), inOneHour));

      _receivedTommandGate.TryAwaitPassedThroughCountEqualTo(1, timeout: .5.Seconds())
                          .Should().Be(false);
   }

   internal class ScheduledTommand : TessageTypes.Remotable.ExactlyOnce.Tommand;
}
