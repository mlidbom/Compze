using System.Threading.Tasks;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions;

interface IInbox
{
   EndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();
}