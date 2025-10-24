using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions.Transport;

namespace Compze.Tessaging.Implementation.MessageHandling.Abstractions;

interface IInbox
{
   HttpEndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task<object?> Receive(TransportMessage.InComing message);
}