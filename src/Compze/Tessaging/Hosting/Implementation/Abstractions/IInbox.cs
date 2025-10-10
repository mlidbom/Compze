using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions;

interface IInbox
{
   EndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();
}