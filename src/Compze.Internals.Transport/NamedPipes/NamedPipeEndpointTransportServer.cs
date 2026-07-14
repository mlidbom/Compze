using Compze.TypeIdentifiers;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Serialization.Internal;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport.NamedPipes;

public static class NamedPipeEndpointTransportServerRegistrar
{
   ///<summary>Registers the named-pipe implementation of the endpoint's one transport server (<see cref="IEndpointTransportServer"/>),<br/>
   /// unless a transport already registered one — guarded so that every communication style's named-pipe transport registration can<br/>
   /// demand the server without conflicting when an endpoint hosts several styles.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransportServerIfNotRegistered(this IComponentRegistrar registrar) =>
      registrar.IsRegistered<IEndpointTransportServerFactory>()
         ? registrar
         : registrar.Register(Singleton.For<IEndpointTransportServerFactory>().CreatedBy(() => new NamedPipeEndpointTransportServerFactory()));
}

///<summary>Creates the <see cref="NamedPipeEndpointTransportServer"/> for an endpoint, assembling the communication styles'<br/>
/// <see cref="INamedPipeRequestHandlerContribution"/>s from the endpoint's container.</summary>
class NamedPipeEndpointTransportServerFactory : IEndpointTransportServerFactory
{
   public IEndpointTransportServer CreateServer(IRootResolver endpointResolver) =>
      new NamedPipeEndpointTransportServer(endpointResolver.ResolveSet<INamedPipeRequestHandlerContribution>(),
                                           endpointResolver.Resolve<InfrastructureQueryExecutor>(),
                                           endpointResolver.Resolve<IRemotableTessageSerializer>(),
                                           endpointResolver.Resolve<ITypeMap>());
}

///<summary>The named-pipe implementation of <see cref="IEndpointTransportServer"/>: one <see cref="NamedPipeTransportServer"/> serving<br/>
/// the union of every communication style's contributed request handlers, plus the infrastructure queries endpoint discovery runs on —<br/>
/// which every endpoint answers no matter what it speaks, so the server registers that handler itself.</summary>
class NamedPipeEndpointTransportServer : IEndpointTransportServer
{
   readonly NamedPipeTransportServer _server;

   internal NamedPipeEndpointTransportServer(IEnumerable<INamedPipeRequestHandlerContribution> contributions,
                                             InfrastructureQueryExecutor infrastructureQueryExecutor,
                                             IRemotableTessageSerializer serializer,
                                             ITypeMap typeMap)
   {
      var handlers = new Dictionary<NamedPipeTransportRequestKind, Func<NamedPipeTransportRequest, Task<string>>>
      {
         [NamedPipeTransportRequestKind.InfrastructureQuery] = NamedPipeInfrastructureQueryHandler.CreateFor(infrastructureQueryExecutor, serializer, typeMap)
      };

      foreach(var contribution in contributions)
      {
         foreach(var (requestKind, handler) in contribution.RequestHandlers)
         {
            State.Assert(!handlers.ContainsKey(requestKind), () => $"Two contributions both claim to handle {requestKind} — every request kind has exactly one handler.");
            handlers.Add(requestKind, handler);
         }
      }

      _server = new NamedPipeTransportServer(handlers);
   }

   public EndpointAddress Address => _server.Address;

   public async Task StartAsync() => await _server.StartAsync().caf();
   public async Task StopAsync() => await _server.StopAsync().caf();
   public async ValueTask DisposeAsync() => await _server.DisposeAsync().caf();
}
