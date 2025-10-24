using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.MessageHandling.Abstractions;

interface IInbox
{
   HttpEndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task<object?> Receive(TransportMessage.InComing message);
}