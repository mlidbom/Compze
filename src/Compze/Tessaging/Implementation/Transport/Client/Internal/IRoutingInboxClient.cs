using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface IRoutingInboxClient
{
    Task ConnectAsync(EndPointAddress remoteEndpointAddress);
    void Start();
    void Stop();

    IInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand);
    IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent);

    Task PostAsync(IAtMostOnceTypermediaTommand tommand);
    Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand);
    Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}