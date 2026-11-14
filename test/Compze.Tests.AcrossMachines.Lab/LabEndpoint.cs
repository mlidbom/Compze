using System.Net;
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

///<summary>The lab endpoint: a database-less endpoint served over TCP, answering <see cref="TueryAskedFromAnotherMachine"/>.<br/>
/// It writes its address to a file once it listens, for a pure client to be told. Given the address of the lab's registry<br/>
/// server, it also participates in the lab registry there (<see cref="LabRegistry"/>), where a conversing endpoint finds it<br/>
/// without being told anything.</summary>
static class LabEndpoint
{
   ///<summary>Hosts the lab endpoint, listening on <paramref name="listenOn"/>, an address of this machine that the machine<br/>
   /// conversing with it can connect to, and writes the endpoint's address to <paramref name="addressFile"/> once it listens.<br/>
   /// With <paramref name="registryServerListensAt"/>, the endpoint participates in the lab registry on the registry server<br/>
   /// there, which need not be up yet. Serves until the process is told to stop (Ctrl+C) or is killed.</summary>
   public static async Task ServeAsync(IPAddress listenOn, FileInfo addressFile, IPEndPoint? registryServerListensAt)
   {
      await using var registry = registryServerListensAt is null ? null : NetworkEndpointRegistry.OpenOnServerAt(LabRegistry.Name, registryServerListensAt);
      var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder());
      await using(host)
      {
         var endpoint = host.RegisterEndpoint(new Declaration(), new Environment(listenOn, registry));
         await host.StartAsync();

         await File.WriteAllTextAsync(addressFile.FullName, endpoint.Address!.ToString());
         Console.WriteLine($"The lab endpoint listens at {endpoint.Address} in {System.Environment.MachineName}.");

         await ToldToStop();
      }
   }

   static Task ToldToStop()
   {
      var toldToStop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      Console.CancelKeyPress += (_, cancelKeyPress) =>
      {
         cancelKeyPress.Cancel = true; //The process ends by returning from Main, with the endpoint stopped, not by being terminated here.
         toldToStop.TrySetResult();
      };
      return toldToStop.Task;
   }

   class Declaration : BestEffortEndpointDeclaration<LabEndpointIdentity>
   {
      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireMappedTypesFromAssemblyContaining<TueryAskedFromAnotherMachine>();

      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery<TueryAskedFromAnotherMachine, AnswerFromTheLabEndpoint>(async (tuery, _) =>
         {
            await Task.Delay(tuery.AnswerAfter);
            return new AnswerFromTheLabEndpoint(answeredByMachine: System.Environment.MachineName, payload: new string('x', tuery.AnswerPayloadCharacters));
         });
   }

   ///<summary>The lab endpoint's environment: the TCP transport listening on the address the program was given, Newtonsoft<br/>
   /// serialization, and participation in the lab registry when the program was given its registry server's address.</summary>
   class Environment : IEndpointEnvironment
   {
      readonly IPAddress _listenOn;
      readonly NetworkEndpointRegistry? _registry;

      internal Environment(IPAddress listenOn, NetworkEndpointRegistry? registry)
      {
         _listenOn = listenOn;
         _registry = registry;
      }

      public void Configure(EndpointBuilder endpointBuilder)
      {
         endpointBuilder.TcpEndpointTransport(_listenOn)
                        .NewtonsoftSerializer();
         if(_registry != null) endpointBuilder.ParticipateIn(_registry);
      }

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) {} //Never called: the lab endpoint is a best-effort endpoint, which has no domain database.
   }
}
