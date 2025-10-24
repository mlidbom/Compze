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

    IInboxConnection ConnectionToHandlerFor(IRemotableCommand command);
    IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceEvent @event);

    Task PostAsync(IAtMostOnceHypermediaCommand command);
    Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command);
    Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query);
}