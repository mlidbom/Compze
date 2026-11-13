using System.Net;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Hosting;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Transport.Tcp;
using Compze.Tessaging.Typermedia;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>The lab endpoint: a database-less endpoint served over TCP, answering <see cref="TueryAskedFromAnotherMachine"/>.<br/>
/// It takes part in no registry, so whoever converses with it must be told its address: it writes the address to a file<br/>
/// once it listens.</summary>
static class LabEndpoint
{
   ///<summary>Hosts the lab endpoint, listening on <paramref name="listenOn"/>, an address of this machine that the machine<br/>
   /// conversing with it can connect to, and writes the endpoint's address to <paramref name="addressFile"/> once it listens.<br/>
   /// Serves until the process is told to stop (Ctrl+C) or is killed.</summary>
   public static async Task ServeAsync(IPAddress listenOn, FileInfo addressFile)
   {
      var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder());
      await using(host)
      {
         var endpoint = host.RegisterEndpoint(new Declaration(), new Environment(listenOn));
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
   /// serialization, and no place in any topology.</summary>
   class Environment : IEndpointEnvironment
   {
      readonly IPAddress _listenOn;
      internal Environment(IPAddress listenOn) => _listenOn = listenOn;

      public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
         .TcpEndpointTransport(_listenOn)
         .NewtonsoftSerializer();

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) {} //Never called: the lab endpoint is a best-effort endpoint, which has no domain database.
   }
}
