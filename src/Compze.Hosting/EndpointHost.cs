using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Hosting;

///<summary>
/// The <see cref="IEndpointHost"/> mechanism: a convenience owning several endpoints' lifecycles in one process. Each
/// endpoint is composed on a fresh container builder from the factory the host was created with
/// (<see cref="IEndpointHost.RegisterEndpoint{TEndpoint}"/>); starting the host starts every endpoint, each driving its own
/// phase ordering (see <see cref="IEndpoint.StartAsync"/>), and disposing it disposes them. The host adds nothing an
/// endpoint cannot do alone — endpoints are first-class — and it never knows what an endpoint can do: that is decided
/// entirely by the endpoint's composition.
///
/// Production hosts are created via <see cref="Production.Create(Func{IContainerBuilder}, IEndpointEnvironment)"/>; tests use the testing host in
/// Compze.Tessaging.Hosting.Testing, which subclasses this mechanism.
///</summary>
public class EndpointHost : IEndpointHost
{
   readonly Func<IContainerBuilder> _containerFactory;
   readonly List<IEndpoint> _endpoints = [];
   public IReadOnlyList<IEndpoint> Endpoints => _endpoints;

   protected EndpointHost(Func<IContainerBuilder> containerFactory) => _containerFactory = containerFactory;

   ///<summary>The one <see cref="IEndpointEnvironment"/> every endpoint of this host runs in by default — what<br/>
   /// <see cref="RegisterEndpoint(IExactlyOnceEndpointDeclaration)"/> builds each declaration in. Public so a composition<br/>
   /// needing the shared environment plus something extra can wrap it in a decorating <see cref="IEndpointEnvironment"/><br/>
   /// and register through <see cref="RegisterEndpoint(IExactlyOnceEndpointDeclaration, IEndpointEnvironment)"/>.<br/>
   /// Reading it on a host created without one fails loud: building an endpoint-declaration takes an environment.</summary>
   public IEndpointEnvironment Environment
   {
      get
      {
         State.Assert(field is not null, () => $"This host was created without an {nameof(IEndpointEnvironment)}, and building an endpoint-declaration takes one: create the host with EndpointHost.Production.Create(containerFactory, environment).");
         return field!;
      }
      protected set;
   }

   public static class Production
   {
      public static IEndpointHost Create(Func<IContainerBuilder> containerFactory) => new EndpointHost(containerFactory);

      ///<summary>Creates a production host that builds every registered endpoint-declaration (<see cref="EndpointDeclaration{TIdentity}"/>) in <paramref name="environment"/>.</summary>
      public static IEndpointHost Create(Func<IContainerBuilder> containerFactory, IEndpointEnvironment environment) =>
         new EndpointHost(containerFactory) { Environment = environment };
   }

   public TEndpoint RegisterEndpoint<TEndpoint>(Func<IContainerBuilder, TEndpoint> composeEndpoint) where TEndpoint : IEndpoint
   {
      var endpoint = composeEndpoint(_containerFactory());
      _endpoints.Add(endpoint);
      return endpoint;
   }

   public ExactlyOnceEndpoint RegisterEndpoint(IExactlyOnceEndpointDeclaration declaration) => RegisterEndpoint(declaration, Environment);

   public BestEffortEndpoint RegisterEndpoint(IBestEffortEndpointDeclaration declaration) => RegisterEndpoint(declaration, Environment);

   public ExactlyOnceEndpoint RegisterEndpoint(IExactlyOnceEndpointDeclaration declaration, IEndpointEnvironment environment) =>
      RegisterEndpoint(containerBuilder => declaration.BuildOn(containerBuilder, environment));

   public BestEffortEndpoint RegisterEndpoint(IBestEffortEndpointDeclaration declaration, IEndpointEnvironment environment) =>
      RegisterEndpoint(containerBuilder => declaration.BuildOn(containerBuilder, environment));

   bool _isStarted;

   public async Task StartAsync()
   {
      State.Assert(!_isStarted, Endpoints.None(endpoint => endpoint.IsRunning));
      this.Log().Info($"Starting with {Endpoints.Count} endpoint(s)");

      await Task.WhenAll(Endpoints.Select(endpointToStart => endpointToStart.StartAsync())).WithAggregateExceptions().caf();
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
         _isStarted = false;
         //Each endpoint's disposal drives its own mirror phases - retract, stop sending, stop listening - before tearing down.
         await Task.WhenAll(Endpoints.Select(endpoint => endpoint.DisposeAsync().AsTask())).WithAggregateExceptions().caf();
      }
   }

   public async ValueTask DisposeAsync() => await DisposeAsync(true).WithAggregateExceptions().caf();
}
