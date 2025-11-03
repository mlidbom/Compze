using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting;

public class EndpointHost : IEndpointHost
{
   readonly IComponentRegistrar _registrar;
   readonly Func<IDependencyInjectionContainer> _containerFactory;
   protected IList<IEndpoint> Endpoints { get; } = [];
   internal ITessagesInFlightTracker TessagesInFlightTracker;

   protected EndpointHost(IComponentRegistrar registrar, Func<IDependencyInjectionContainer> containerFactory)
   {
      _registrar = registrar;
      _containerFactory = containerFactory;
      TessagesInFlightTracker = new NullOpTessagesInFlightTracker();
   }

   public static class Production
   {
      public static IEndpointHost Create(Func<IDependencyInjectionContainer> containerFactory) => new EndpointHost(new ComponentRegistrar(), containerFactory);
   }

   public virtual IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup) => InternalRegisterEndpoint(new EndpointConfiguration(name, id, isPureClientEndpoint: false), setup);

   IEndpoint InternalRegisterEndpoint(EndpointConfiguration configuration, Action<IEndpointBuilder> setup)
   {
      using var builder = new ServerEndpointBuilder(this, TessagesInFlightTracker, _containerFactory(), configuration);
      setup(builder);

      var endpoint = builder.Build();

      Endpoints.Add(endpoint);
      return endpoint;
   }

   static readonly EndpointConfiguration ClientEndpointConfiguration = new(name: $"{nameof(EndpointHost)}_Default_Client_Endpoint",
                                                                           id: new EndpointId(Guid.Parse("D4C869D2-68EF-469C-A5D6-37FCF2EC152A")),
                                                                           isPureClientEndpoint: true);

   public virtual IEndpoint RegisterClientEndpoint(Action<IEndpointBuilder> setup) => InternalRegisterEndpoint(ClientEndpointConfiguration, setup);

   bool _isStarted;

   public async Task StartAsync()
   {
      try
      {
         Assert.State.Is(!_isStarted).Is(Endpoints.None(endpoint => endpoint.IsRunning));
         _isStarted = true;

         await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.StartListeningComponentsAsync())).WithAggregateExceptions().caf();
         await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.StartSendingComponentsAsync())).WithAggregateExceptions().caf();
      }catch(Exception e)
      {
         this.Log().Error(e, "Failed to start host");
         await DisposeAsync().caf();
         throw;
      }
   }

   public void Start() => StartAsync().WaitUnwrappingException();

   bool _disposed;
   protected virtual async ValueTask DisposeAsync(bool disposing)
   {
      if(!_disposed)
      {
         _disposed = true;
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
