using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Internals.Transport.NamedPipes;

public static class NamedPipeEndpointTransportRegistrar
{
   ///<summary>Declares the endpoint's transport protocol: named pipes. Registers the named-pipe endpoint transport client<br/>
   /// (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query transport that runs on it, and the endpoint's one<br/>
   /// transport server (<see cref="IEndpointTransportServer"/>) serving every communication style's contributed request handlers —<br/>
   /// the same-machine protocol, with no web stack. The communication styles themselves register nothing protocol-specific.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransport(this IComponentRegistrar registrar)
      => registrar.NamedPipeEndpointTransportClientIfNotRegistered()
                  .EndpointDiscoveryQueryTransportIfNotRegistered()
                  .NamedPipeEndpointTransportServerIfNotRegistered();

   extension(EndpointComposer @this)
   {
      ///<summary>Declares the endpoint's transport protocol: named pipes — see <see cref="NamedPipeEndpointTransport(IComponentRegistrar)"/>,<br/>
      /// to which this delegates. Returns the endpoint's foundation (<see cref="EndpointFoundation"/>), on which the database is<br/>
      /// declared and the distributed features are added.</summary>
      public EndpointFoundation NamedPipeEndpointTransport()
      {
         @this.Builder.Registrar.NamedPipeEndpointTransport();
         return new EndpointFoundation(@this.Builder);
      }
   }
}
