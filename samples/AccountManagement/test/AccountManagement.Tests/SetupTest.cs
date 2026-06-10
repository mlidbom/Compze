using Compze.Hosting.Testing;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Hosting.Testing;
using Compze.Typermedia.Client;
using Compze.Typermedia.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

namespace AccountManagement;

public class SetupTest : UniversalTestBase
{
   [PCT] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create(new TessagingTestingEndpointHostFeature(), new TypermediaTestingEndpointHostFeature());
      var endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(host);
      await host.StartAsync().caf();
      await using var client = await TypermediaTestClient.ConnectTo(endpoint.TypermediaAddress!, mapper => mapper.RegisterAccountManagementTypeMappings()).caf();
      await host.DisposeAsync().caf();
   }
}
