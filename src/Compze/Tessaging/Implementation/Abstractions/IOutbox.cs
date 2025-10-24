using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Implementation.Abstractions;

interface IOutbox
{
    Task StartAsync();
    Task StopAsync();
    void PublishTransactionally(IExactlyOnceEvent exactlyOnceEvent);
    void SendTransactionally(IExactlyOnceCommand exactlyOnceCommand);
}