using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

public interface ITypermediaRouter
{
    Task ConnectAsync(EndPointAddress remoteEndpointAddress);
    Task DiscoverAndConnectAsync(EndPointAddress seedAddress);
    void Start();
    void Stop();

    Task PostAsync(IAtMostOnceTypermediaTommand tommand);
    Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand);
    Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}
