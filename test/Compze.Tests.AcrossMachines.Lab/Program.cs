using System.Net;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>The across-machines lab's program: what runs on each side when Compze's endpoints are tried across machines.<br/>
/// In one machine it hosts the lab endpoint over TCP (<c>serve</c>, <see cref="LabEndpoint"/>); in another it holds a<br/>
/// <see cref="Conversation"/> with that endpoint and reports what each tuery experienced: as a pure client that was told<br/>
/// the endpoint's address (<c>converse</c>), or as an endpoint that finds it through a registry across machines<br/>
/// (<c>converse-through-registry</c>).<br/>
/// The C-VM-* commands of DevScripts run the serving side in the lab machine, a Hyper-V virtual machine, whose guest has<br/>
/// the .NET runtime and no ASP.NET Core runtime: that this program runs there shows the TCP transport needs none.</summary>
public static class Program
{
   const string Usage = """
                        The across-machines lab's program. Its commands:

                          serve <address of this machine to listen on> <file to write the lab endpoint's address to>
                                [<the registry server's address>:<port>]
                              Hosts the lab endpoint over TCP until stopped. With a registry server's address, the endpoint
                              participates in the lab registry there, whether or not the server is up yet.

                          converse <the lab endpoint's address> [options]
                              As a pure client told the lab endpoint's address: asks it tueries, round after round, and
                              reports what each one experienced.

                          converse-through-registry <address of this machine to listen on>:<port> [options]
                              Hosts the registry server at that address and port, and an endpoint that finds the lab
                              endpoint through the lab registry. Then asks and reports as converse does.

                        The options of both ways to converse:
                          --for <seconds>  --pause-between-rounds <milliseconds>  --tueries-per-round <count>
                          --answer-payload-characters <count>  --answer-after <milliseconds>
                        """;

   public static async Task<int> Main(string[] args)
   {
      switch(args)
      {
         case ["serve", var listenOn, var addressFile]:
            await LabEndpoint.ServeAsync(IPAddress.Parse(listenOn), new FileInfo(addressFile), registryServerListensAt: null);
            return 0;
         case ["serve", var listenOn, var addressFile, var registryServerListensAt]:
            await LabEndpoint.ServeAsync(IPAddress.Parse(listenOn), new FileInfo(addressFile), IPEndPoint.Parse(registryServerListensAt));
            return 0;
         case ["converse", var labEndpointAddress, .. var options]:
            await ConversingPureClient.HoldConversationAsync(new EndpointAddress(new Uri(labEndpointAddress)), ConversationOptions.Parse(options));
            return 0;
         case ["converse-through-registry", var registryServerListensAt, .. var options]:
            await ConversingEndpoint.HoldConversationAsync(IPEndPoint.Parse(registryServerListensAt), ConversationOptions.Parse(options));
            return 0;
         default:
            await Console.Error.WriteLineAsync(Usage);
            return 1;
      }
   }
}
