using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Hosting;

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

   public virtual IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup) => InternalRegisterEndpoint(new EndpointConfiguration(name, id), setup);

   IEndpoint InternalRegisterEndpoint(EndpointConfiguration configuration, Action<IEndpointBuilder> setup)
   {
      var builder = new ServerEndpointBuilder(_containerFactory(), configuration);
      setup(builder);

      var endpoint = builder.Build();

      _endpoints.Add(endpoint);
      return endpoint;
   }

   bool _isStarted;

   public async Task StartAsync()
   {
         State.Assert(!_isStarted, Endpoints.None(endpoint => endpoint.IsRunning));
         this.Log().Info($"Starting with {Endpoints.Count} endpoint(s)");

         await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.StartListeningComponentsAsync())).WithAggregateExceptions().caf();
         await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.StartSendingComponentsAsync())).WithAggregateExceptions().caf();
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
            await Task.WhenAll(Endpoints.Select(endpoint => endpoint.StopSendingComponentsAsync())).WithAggregateExceptions().caf();
            await Task.WhenAll(Endpoints.Select(endpoint => endpoint.StopListeningComponentsAsync())).WithAggregateExceptions().caf();
         }

         await Task.WhenAll(Endpoints.Select(endpoint => endpoint.DisposeAsync().AsTask())).WithAggregateExceptions().caf();
      }
   }

   public async ValueTask DisposeAsync() => await DisposeAsync(true).WithAggregateExceptions().caf();
}
