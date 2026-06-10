using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection.Abstractions;
using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Hosting;

///<summary>
/// The <see cref="IEndpoint"/> mechanism: owns the endpoint's container and drives its
/// <see cref="IEndpointComponent"/>s through the listening/sending phases. Components are created lazily when
/// listening starts — by then the container is built, so component factories can resolve whatever they need.
/// On dispose the container goes first, then any components that implement <see cref="IAsyncDisposable"/>.
///</summary>
class Endpoint : IEndpoint
{
   readonly EndpointConfiguration _configuration;
   readonly IDependencyInjectionContainer _container;
   readonly IRootResolver _rootResolver;
   readonly IReadOnlyList<Func<IRootResolver, IEndpointComponent>> _componentFactories;

   public Endpoint(IDependencyInjectionContainer container, EndpointConfiguration configuration, IReadOnlyList<Func<IRootResolver, IEndpointComponent>> componentFactories)
   {
      Argument.NotNull(container).NotNull(configuration);
      _container = container;
      _rootResolver = container.RootResolver;
      _configuration = configuration;
      _componentFactories = componentFactories;
   }

   public EndpointId Id => _configuration.Id;
   public IRootResolver ServiceLocator => _rootResolver;

   public IReadOnlyList<IEndpointComponent> Components { get; private set; } = [];

   public bool IsRunning => _isListening && _isSending;
   bool _isListening;
   bool _isSending;

   public async Task StartListeningComponentsAsync()
   {
      State.Assert(!_isListening);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) starting listening components");
      _isListening = true;

      Components = _componentFactories.Select(createComponent => createComponent(_rootResolver)).ToList();
      await Task.WhenAll(Components.Select(component => component.StartListeningAsync())).caf();
   }

   public async Task StartSendingComponentsAsync()
   {
      State.Assert(!_isSending);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) starting sending components");
      _isSending = true;
      await Task.WhenAll(Components.Select(component => component.StartSendingAsync())).caf();
   }

   public async Task StopSendingComponentsAsync()
   {
      if(!_isSending) return;
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) stopping sending components");
      _isSending = false;
      await Task.WhenAll(Components.Select(component => component.StopSendingAsync())).caf();
   }

   public async Task StopListeningComponentsAsync()
   {
      if(!_isListening) return;
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) stopping listening components");
      _isListening = false;
      await Task.WhenAll(Components.Select(component => component.StopListeningAsync())).caf();
   }

   public async ValueTask DisposeAsync()
   {
      this.Log().Debug($"Endpoint '{_configuration.Name}' ({Id}) disposing");
      await StopSendingComponentsAsync().caf();
      await StopListeningComponentsAsync().caf();
      await _container.DisposeAsync().caf();
      foreach(var component in Components.OfType<IAsyncDisposable>())
         await component.DisposeAsync().caf();
   }
}
