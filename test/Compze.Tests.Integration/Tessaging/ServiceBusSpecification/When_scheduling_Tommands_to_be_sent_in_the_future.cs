using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Time.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Must;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification;

public class When_scheduling_tommands_to_be_sent_in_the_future : UniversalTestBase
{
   readonly IThreadGate _receivedTommandGate;
   readonly ITestingEndpointHost _host;
   readonly IEndpoint _endpoint;

   public When_scheduling_tommands_to_be_sent_in_the_future()
   {
      _host = TestingEndpointHost.Create();
      _receivedTommandGate = ThreadGate.Open(WaitTimeout.Seconds(1), "receivedTommand");
      _endpoint = _host.RegisterEndpoint(
         "endpoint",
         new EndpointId(Guid.Parse("17ED9DF9-33A8-4DF8-B6EC-6ED97AB2030B")),
         builder =>
         {
            builder.RegisterHandlers.ForTommand<ScheduledTommand>(_ => _receivedTommandGate.AwaitPassThrough());
         });
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT]  public void Tessages_whose_due_time_has_passed_are_delivered()
   {
      var now = UtcTimeSource.UtcNow;
      var inOneHour = new ScheduledTommand();

      _endpoint.ExecuteServerRequestInTransaction(session => session.ScheduleSend(now + .2.Seconds(), inOneHour));

      _receivedTommandGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(10));
   }

   [PCT]  public void Tessages_whose_due_time_have_not_passed_are_not_delivered()
   {
      var now = UtcTimeSource.UtcNow;
      var inOneHour = new ScheduledTommand();
      _endpoint.ExecuteServerRequestInTransaction(session => session.ScheduleSend(now + 2.Seconds(), inOneHour));

      _receivedTommandGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(500))
                          .Must().Be(false);
   }

   internal class ScheduledTommand : TessageTypes.Remotable.ExactlyOnce.Tommand;
}
