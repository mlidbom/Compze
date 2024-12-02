using System;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.Messaging;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;
using Compze.Persistence.Common.DependencyInjection;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.Messaging.Buses;
using Compze.Testing.Persistence;
using NUnit.Framework;

//ncrunch: no coverage start

namespace Compze.Tests.Messaging.Hypermedia;

public class PerformanceTestBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected ITestingEndpointHost Host { get; set; }
   protected IEndpoint ServerEndpoint { get; set; }
   public IEndpoint ClientEndpoint { get; set; }
   protected IRemoteHypermediaNavigator RemoteNavigator => ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();
   protected IServiceBusSession ServerBusSession => ServerEndpoint.ServiceLocator.Resolve<IServiceBusSession>();
   protected ILocalHypermediaNavigator LocalNavigator => ServerEndpoint.ServiceLocator.Resolve<ILocalHypermediaNavigator>();

   [SetUp] public async Task Setup()
   {
      Host = TestingEndpointHost.Create(TestingContainerFactory.Create);
      ServerEndpoint = Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
         builder =>
         {
            builder.RegisterCurrentTestsConfiguredPersistenceLayer();
            builder.RegisterHandlers
                   .ForQuery((MyRemoteQuery _) => new MyQueryResult())
                   .ForQuery((MyLocalStrictlyLocalQuery _) => new MyQueryResult());

            builder.TypeMapper
                   .Map<MyRemoteQuery>("b9d62f22-514b-4e3c-9ac1-66940a7a8144")
                   .Map<MyLocalStrictlyLocalQuery>("5640cfb1-0dbc-4e2b-9915-b5b91a289e86")
                   .Map<MyQueryResult>("07e144ab-af3c-4c2c-9d83-492deffd24aa");
         });

      ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();
      await Host.StartAsync().CaF();
   }

   [TearDown] public async Task TearDown() => await Host.DisposeAsync().CaF();

   protected class MyRemoteQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<MyQueryResult>;
   protected class MyLocalStrictlyLocalQuery : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<MyLocalStrictlyLocalQuery, MyQueryResult>;
   protected internal class MyQueryResult;
}