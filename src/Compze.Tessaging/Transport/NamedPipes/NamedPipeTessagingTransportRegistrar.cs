using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.NamedPipes;

namespace Compze.Tessaging.Transport.NamedPipes;

public static class NamedPipeTessagingTransportRegistrar
{
   ///<summary>
   /// Registers the named-pipe implementation of the Tessaging transport: the inbox server that receives tessages and answers
   /// infrastructure queries, and the client that posts tessages. The same-machine counterpart of the ASP.NET Core Tessaging
   /// transport, with no web stack — requires the named-pipe infrastructure-query transport
   /// (<see cref="Compze.Internals.Transport.NamedPipes.NamedPipeInfrastructureQueryTransportRegistrar"/>) to be registered by
   /// the composing layer.
   ///</summary>
   public static IComponentRegistrar NamedPipeTessagingTransport(this IComponentRegistrar registrar) =>
      registrar.Register(NamedPipeTransportMessagePoster.RegisterWith,
                         NamedPipeInboxTransportServer.RegisterWith);
}
