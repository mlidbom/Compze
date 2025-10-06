using System;
using System.Threading.Tasks;
using Compze.DependencyInjection;

namespace Compze.Hosting.Abstractions;

public interface IEndpoint : IAsyncDisposable
{
    EndpointId Id { get; }
    IServiceLocator ServiceLocator { get; }
    EndPointAddress? Address { get; }
    bool IsRunning { get; }
    Task InitAsync();
    Task ConnectAsync();
    Task StopAsync();
    void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
}
