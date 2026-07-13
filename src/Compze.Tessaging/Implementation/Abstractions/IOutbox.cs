using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Abstractions;

interface IOutbox
{
    Task StartAsync();
    Task StopAsync();
    ///<summary>Publishes the wrapped tevent to every remote subscriber - the whole wrapper travels the wire, so publisher identity crosses endpoints with zero information loss.<br/>
    /// The wrapper carries only publisher identity, not delivery-guarantee markers; the dedup identity is the wrapped tevent's own <see cref="IAtMostOnceTessage.Id"/>, read from <see cref="IPublisherIdentifyingTevent{TTevent}.Tevent"/>.</summary>
    void PublishTransactionally(IPublisherIdentifyingTevent<IExactlyOnceTevent> wrappedTevent);
    void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand);
}