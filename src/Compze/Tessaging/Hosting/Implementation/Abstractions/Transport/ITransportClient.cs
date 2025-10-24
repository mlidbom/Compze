using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions.Transport;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions.Transport;

interface ITransportClient
{
    Task ConnectAsync(EndPointAddress remoteEndpointAddress);
    void Start();
    void Stop();

    IInboxConnection ConnectionToHandlerFor(IRemotableCommand command);
    IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceEvent @event);

    Task PostAsync(IAtMostOnceHypermediaCommand command);
    Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command);
    Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query);
}