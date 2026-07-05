using Compze.Abstractions.Tessaging.Public;

namespace Compze.ServiceBus.Implementation.Abstractions;

interface IOutbox
{
    Task StartAsync();
    Task StopAsync();
    void PublishTransactionally(IExactlyOnceTevent exactlyOnceTevent);
    void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand);
}