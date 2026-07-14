using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Internals.Transport.NamedPipes;
using Compze.Tessaging.Implementation.Transport.Client.Implementation;

namespace Compze.Tessaging.Transport.NamedPipes;

public static class NamedPipeTessagingTransportRegistrar
{
   ///<summary>
   /// Registers the Tessaging transport speaking named pipes: the client that posts tessages, Tessaging's
   /// request handling contributed to the endpoint's one named-pipe transport server (registering the server itself if no
   /// other communication style already did — the server also answers endpoint-discovery queries),
   /// and the named-pipe endpoint transport client plus the endpoint-discovery query transport that runs on it (both shared
   /// with every other communication style, so registered only if nothing else did yet). The same-machine counterpart of the
   /// ASP.NET Core Tessaging transport, with no web stack.
   ///</summary>
   public static IComponentRegistrar NamedPipeTessagingTransport(this IComponentRegistrar registrar) =>
      registrar.NamedPipeEndpointTransportClientIfNotRegistered()
               .EndpointDiscoveryQueryTransportIfNotRegistered()
               .NamedPipeEndpointTransportServerIfNotRegistered()
               .Register(TransportMessagePoster.RegisterWith,
                         NamedPipeTessagingRequestHandlers.RegisterWith);
}
