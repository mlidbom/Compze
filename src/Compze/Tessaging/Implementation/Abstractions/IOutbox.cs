using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Abstractions;

interface IOutbox
{
    Task StartAsync();
    Task StopAsync();
    void PublishTransactionally(IExactlyOnceTevent exactlyOnceTevent);
    void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand);
}