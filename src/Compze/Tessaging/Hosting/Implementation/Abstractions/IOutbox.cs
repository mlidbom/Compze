using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions;

interface IOutbox
{
    Task StartAsync();
    void PublishTransactionally(IExactlyOnceEvent exactlyOnceEvent);
    void SendTransactionally(IExactlyOnceCommand exactlyOnceCommand);
}