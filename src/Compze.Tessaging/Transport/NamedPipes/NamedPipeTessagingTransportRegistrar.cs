using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport.NamedPipes;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.NamedPipes;

namespace Compze.Tessaging.Transport.NamedPipes;

public static class NamedPipeTessagingTransportRegistrar
{
   ///<summary>
   /// Registers the named-pipe implementation of the Tessaging transport: the client that posts tessages, Tessaging's
   /// request handling contributed to the endpoint's one named-pipe transport server (registering the server itself if no
   /// other communication style already did — the server also answers endpoint-discovery queries),
   /// and the named-pipe endpoint-discovery query transport this endpoint queries other endpoints through (shared with every
   /// other named-pipe communication style, so registered only if nothing else did yet). The same-machine counterpart of the
   /// ASP.NET Core Tessaging transport, with no web stack.
   ///</summary>
   public static IComponentRegistrar NamedPipeTessagingTransport(this IComponentRegistrar registrar) =>
      registrar.NamedPipeEndpointDiscoveryQueryTransportIfNotRegistered()
               .NamedPipeEndpointTransportServerIfNotRegistered()
               .Register(NamedPipeTransportMessagePoster.RegisterWith,
                         NamedPipeTessagingRequestHandlers.RegisterWith);
}
