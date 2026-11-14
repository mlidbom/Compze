using System.Diagnostics;
using Compze.DependencyInjection.Microsoft;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Transport.Tcp;
using Compze.Tessaging.Typermedia.Client;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>The conversing side as a pure client (<see cref="TypermediaClient"/>) over TCP that was told the lab endpoint's<br/>
/// address: it takes part in no registry, and connects to that one address.</summary>
static class ConversingPureClient
{
   ///<summary>Connects to the lab endpoint at <paramref name="labEndpointAddress"/>, then holds the <see cref="Conversation"/>.</summary>
   public static async Task HoldConversationAsync(EndpointAddress labEndpointAddress, ConversationOptions options)
   {
      await using var client = TypermediaClient.Build(new MicrosoftContainerBuilder(),
                                                      builder =>
                                                      {
                                                         builder.ConfigureTransport(registrar => registrar.TcpEndpointTransportClient())
                                                                .DeclareRequiredTypeMappings(registrar => registrar.RequireMappedTypesFromAssemblyContaining<TueryAskedFromAnotherMachine>());
                                                         builder.NewtonsoftSerializer();
                                                      });

      var connecting = Stopwatch.StartNew();
      await client.ConnectAsync(labEndpointAddress);
      LabReport.Line($"connected to {labEndpointAddress} in {LabReport.MillisecondsOf(connecting)} ms");

      await new Conversation(client.Navigator, options).HoldAsync();
   }
}
