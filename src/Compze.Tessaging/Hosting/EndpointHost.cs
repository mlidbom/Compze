using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Contracts;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Hosting;

public class EndpointHost : IEndpointHost
{
   readonly Func<IDependencyInjectionContainer> _containerFactory;
   protected IList<IEndpoint> Endpoints { get; } = [];
   internal ITessagesInFlightTracker TessagesInFlightTracker;

   protected EndpointHost(Func<IDependencyInjectionContainer> containerFactory)
   {
      _containerFactory = containerFactory;
      TessagesInFlightTracker = new NullOpTessagesInFlightTracker();
   }

   public static class Production
   {
      public static IEndpointHost Create(Func<IDependencyInjectionContainer> containerFactory) => new EndpointHost(containerFactory);
   }

   public virtual IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup) => InternalRegisterEndpoint(new EndpointConfiguration(name, id), setup);

   IEndpoint InternalRegisterEndpoint(EndpointConfiguration configuration, Action<IEndpointBuilder> setup)
   {
      using var builder = new ServerEndpointBuilder(this, TessagesInFlightTracker, _containerFactory(), configuration);
      setup(builder);

      var endpoint = builder.Build();

      Endpoints.Add(endpoint);
      return endpoint;
   }

   bool _isStarted;

   public async Task StartAsync()
   {
         Contract.State.Assert(!_isStarted, Endpoints.None(endpoint => endpoint.IsRunning));
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
