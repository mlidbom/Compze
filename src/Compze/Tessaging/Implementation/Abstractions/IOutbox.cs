using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Abstractions;

interface IOutbox
{
    Task StartAsync();
    Task StopAsync();
    void PublishTransactionally(IExactlyOnceEvent exactlyOnceEvent);
    void SendTransactionally(IExactlyOnceCommand exactlyOnceCommand);
}