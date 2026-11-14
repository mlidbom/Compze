using System.Diagnostics;
using System.Net;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Hosting;
using Compze.Hosting.AcrossMachines;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Transport.Tcp;
using Compze.Tessaging.Typermedia;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>The conversing side as an endpoint that is told no address: it hosts the lab's <see cref="EndpointRegistryServer"/>,<br/>
/// participates in the lab registry on it (<see cref="LabRegistry"/>), and finds the lab endpoint there once the lab<br/>
/// endpoint, in the other machine, has announced itself. It is what a specification process on a Hyper-V host is to the<br/>
/// endpoints in its guest.</summary>
static class ConversingEndpoint
{
   static readonly TimeSpan PatienceWithFindingTheLabEndpoint = TimeSpan.FromMinutes(5);

   ///<summary>Starts the registry server at <paramref name="registryServerListensAt"/>, an address of this machine that the<br/>
   /// other machine can connect to, and the conversing endpoint on the same IP address. Waits until the lab endpoint is<br/>
   /// found through the registry, then holds the <see cref="Conversation"/>.</summary>
   public static async Task HoldConversationAsync(IPEndPoint registryServerListensAt, ConversationOptions options)
   {
      await using var registryServer = EndpointRegistryServer.StartListeningAt(registryServerListensAt);
      LabReport.Line($"the registry server listens at {registryServer.ListensAt} in {System.Environment.MachineName}");
      await using var registry = NetworkEndpointRegistry.OpenOnServerAt(LabRegistry.Name, registryServer.ListensAt);

      var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder());
      await using(host)
      {
         var endpoint = host.RegisterEndpoint(new Declaration(), new Environment(listenOn: registryServerListensAt.Address, registry));
         await host.StartAsync();

         var finding = Stopwatch.StartNew();
         await endpoint.AwaitReadinessAsync(ReadinessTypes.These(typeof(TueryAskedFromAnotherMachine)), PatienceWithFindingTheLabEndpoint);
         LabReport.Line($"found the lab endpoint through the registry in {LabReport.MillisecondsOf(finding)} ms");

         await new Conversation(endpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>(), options).HoldAsync();
      }
   }

   class Declaration : BestEffortEndpointDeclaration<ConversingEndpointIdentity>
   {
      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireMappedTypesFromAssemblyContaining<TueryAskedFromAnotherMachine>();
   }

   ///<summary>The conversing endpoint's environment: the TCP transport listening on the address the registry server<br/>
   /// listens on, Newtonsoft serialization, and participation in the lab registry.</summary>
   class Environment : IEndpointEnvironment
   {
      readonly IPAddress _listenOn;
      readonly NetworkEndpointRegistry _registry;

      internal Environment(IPAddress listenOn, NetworkEndpointRegistry registry)
      {
         _listenOn = listenOn;
         _registry = registry;
      }

      public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
         .TcpEndpointTransport(_listenOn)
         .NewtonsoftSerializer()
         .ParticipateIn(_registry);

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) {} //Never called: the conversing endpoint is a best-effort endpoint, which has no domain database.
   }
}
