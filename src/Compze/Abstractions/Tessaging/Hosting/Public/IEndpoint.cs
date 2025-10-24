using System;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Tessaging.Hosting.Public;

public interface IEndpoint : IAsyncDisposable
{
    EndpointId Id { get; }
    IServiceLocator ServiceLocator { get; }
    HttpEndPointAddress? Address { get; }
    bool IsRunning { get; }
    Task StartListeningComponentsAsync();
    Task StartSendingComponentsAsync();
    Task StopListeningComponentsAsync();
    void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    Task StopSendingComponentsAsync();
}
