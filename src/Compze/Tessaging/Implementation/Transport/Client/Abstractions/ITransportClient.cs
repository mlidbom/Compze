using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface ITransportClient
{
    Task ConnectAsync(HttpEndPointAddress remoteEndpointAddress);
    void Start();
    void Stop();

    IInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand);
    IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent);

    Task PostAsync(IAtMostOnceHypermediaTommand tommand);
    Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceTommand<TCommandResult> tommand);
    Task<TQueryResult> GetAsync<TQueryResult>(IRemotableTuery<TQueryResult> tuery);
}