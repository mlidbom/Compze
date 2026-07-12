using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Abstractions;

interface IOutbox
{
    Task StartAsync();
    Task StopAsync();
    ///<summary>Publishes the wrapped tevent to every remote subscriber - the whole wrapper travels the wire, so publisher identity crosses endpoints with zero information loss.<br/>
    /// The wrapper is itself the <see cref="IExactlyOnceTevent"/> the outbox stores and delivers: its <see cref="IAtMostOnceTessage.Id"/> is the wrapped tevent's.</summary>
    void PublishTransactionally(IExactlyOncePublisherIdentifyingTevent<IExactlyOnceTevent> wrappedTevent);
    void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand);
}