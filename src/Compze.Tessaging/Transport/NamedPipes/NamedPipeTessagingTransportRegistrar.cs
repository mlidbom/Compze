using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport.NamedPipes;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.NamedPipes;

namespace Compze.Tessaging.Transport.NamedPipes;

public static class NamedPipeTessagingTransportRegistrar
{
   ///<summary>
   /// Registers the named-pipe implementation of the Tessaging transport: the client that posts tessages, and Tessaging's
   /// request handling contributed to the endpoint's one named-pipe transport server (registering the server itself if no
   /// other communication style already did — the server also answers the infrastructure queries endpoint discovery runs on).
   /// The same-machine counterpart of the ASP.NET Core Tessaging transport, with no web stack — requires the named-pipe
   /// infrastructure-query transport
   /// (<see cref="Compze.Internals.Transport.NamedPipes.NamedPipeInfrastructureQueryTransportRegistrar"/>) to be registered by
   /// the composing layer.
   ///</summary>
   public static IComponentRegistrar NamedPipeTessagingTransport(this IComponentRegistrar registrar) =>
      registrar.NamedPipeEndpointTransportServerIfNotRegistered()
               .Register(NamedPipeTransportMessagePoster.RegisterWith,
                         NamedPipeTessagingRequestHandlers.RegisterWith);
}
