using System.Net;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>The across-machines lab's program: what runs on each side when Compze's endpoints are tried across machines.<br/>
/// In one machine it hosts the lab endpoint over TCP (<c>serve</c>, <see cref="LabEndpoint"/>); in another it holds a<br/>
/// <see cref="Conversation"/> with that endpoint (<c>converse</c>) and reports what each tuery experienced.<br/>
/// The C-VM-* commands of DevScripts run the serving side in the lab machine, a Hyper-V virtual machine, whose guest has<br/>
/// the .NET runtime and no ASP.NET Core runtime: that this program runs there shows the TCP transport needs none.</summary>
public static class Program
{
   const string Usage = """
                        The across-machines lab's program. Its commands:

                          serve <address of this machine to listen on> <file to write the lab endpoint's address to>
                              Hosts the lab endpoint over TCP until stopped.

                          converse <the lab endpoint's address> [--for <seconds>] [--pause-between-rounds <milliseconds>]
                                   [--tueries-per-round <count>] [--answer-payload-characters <count>] [--answer-after <milliseconds>]
                              Asks the lab endpoint tueries, round after round, and reports what each one experienced.
                        """;

   public static async Task<int> Main(string[] args)
   {
      switch(args)
      {
         case ["serve", var listenOn, var addressFile]:
            await LabEndpoint.ServeAsync(IPAddress.Parse(listenOn), new FileInfo(addressFile));
            return 0;
         case ["converse", var labEndpointAddress, .. var options]:
            await new Conversation(new EndpointAddress(new Uri(labEndpointAddress)), ConversationOptions.Parse(options)).HoldAsync();
            return 0;
         default:
            await Console.Error.WriteLineAsync(Usage);
            return 1;
      }
   }
}
