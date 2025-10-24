using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions.Transport;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions.MessageHandling;

interface IInbox
{
   EndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task<object?> Receive(TransportMessage.InComing message);
}