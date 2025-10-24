using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions.Transport;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Abstractions;

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
