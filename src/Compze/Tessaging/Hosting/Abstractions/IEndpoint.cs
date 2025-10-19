using System;
using System.Threading.Tasks;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Abstractions;

public interface IEndpoint : IAsyncDisposable
{
    EndpointId Id { get; }
    IServiceLocator ServiceLocator { get; }
    EndPointAddress? Address { get; }
    bool IsRunning { get; }
    Task StartListeningComponentsAsync();
    Task StartSendingComponentsAsync();
    Task StopListeningComponentsAsync();
    void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    Task StopSendingComponentsAsync();
}
