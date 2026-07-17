using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Hosting;

///<summary>
/// The <see cref="IEndpointHost"/> mechanism. Each endpoint is composed on a fresh container builder from the factory the
/// host was created with (<see cref="IEndpointHost.RegisterEndpoint{TEndpoint}"/>), and the host guarantees the host-wide
/// phase ordering — every endpoint starts listening before any endpoint announces its address, and every address is
/// announced before any endpoint starts sending (see <see cref="IEndpoint"/>). What an endpoint can actually do is decided
/// by its composition; the host never knows.
///
/// Production hosts are created via <see cref="Production.Create"/>; tests use the testing host in
/// Compze.Tessaging.Hosting.Testing, which subclasses this mechanism.
///</summary>
public class EndpointHost : IEndpointHost
{
   readonly Func<IContainerBuilder> _containerFactory;
   readonly List<IEndpoint> _endpoints = [];
   public IReadOnlyList<IEndpoint> Endpoints => _endpoints;

   protected EndpointHost(Func<IContainerBuilder> containerFactory) => _containerFactory = containerFactory;

   public static class Production
   {
      public static IEndpointHost Create(Func<IContainerBuilder> containerFactory) => new EndpointHost(containerFactory);
   }

   public TEndpoint RegisterEndpoint<TEndpoint>(Func<IContainerBuilder, TEndpoint> composeEndpoint) where TEndpoint : IEndpoint
   {
      var endpoint = composeEndpoint(_containerFactory());
      _endpoints.Add(endpoint);
      return endpoint;
   }

   bool _isStarted;

   public async Task StartAsync()
   {
         State.Assert(!_isStarted, Endpoints.None(endpoint => endpoint.IsRunning));
         this.Log().Info($"Starting with {Endpoints.Count} endpoint(s)");

         await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.StartListeningAsync())).WithAggregateExceptions().caf();
         await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.AnnounceAddressAsync())).WithAggregateExceptions().caf();
         await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.StartSendingAsync())).WithAggregateExceptions().caf();
         _isStarted = true;
   }

   public void Start() => StartAsync().WaitUnwrappingException();

   bool _disposed;
   protected virtual async ValueTask DisposeAsync(bool disposing)
   {
      if(!_disposed)
      {
         _disposed = true;
         this.Log().Info("Disposing");
         if(_isStarted)
         {
            _isStarted = false;
            await Task.WhenAll(Endpoints.Select(endpoint => endpoint.RetractAddressAsync())).WithAggregateExceptions().caf();
            await Task.WhenAll(Endpoints.Select(endpoint => endpoint.StopSendingAsync())).WithAggregateExceptions().caf();
            await Task.WhenAll(Endpoints.Select(endpoint => endpoint.StopListeningAsync())).WithAggregateExceptions().caf();
         }

         await Task.WhenAll(Endpoints.Select(endpoint => endpoint.DisposeAsync().AsTask())).WithAggregateExceptions().caf();
      }
   }

   public async ValueTask DisposeAsync() => await DisposeAsync(true).WithAggregateExceptions().caf();
}
