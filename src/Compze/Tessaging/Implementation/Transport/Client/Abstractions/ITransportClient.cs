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
    Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> tommand);
    Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}