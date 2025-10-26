using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface IEndpoint : IAsyncDisposable
{
    EndpointId Id { get; }
    IServiceLocator ServiceLocator { get; }
    EndPointAddress? Address { get; }
    bool IsRunning { get; }
    Task StartListeningComponentsAsync();
    Task StartSendingComponentsAsync();
    Task StopListeningComponentsAsync();
    void AwaitNoTessagesInFlight(TimeSpan? timeoutOverride);
    Task StopSendingComponentsAsync();
}
