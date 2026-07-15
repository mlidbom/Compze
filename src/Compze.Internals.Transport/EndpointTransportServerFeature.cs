using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Transport;

///<summary>The endpoint's one transport server, as a feature: whichever distributed communication style is added to an endpoint<br/>
/// first brings this feature with it (idempotently, via <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>), further styles<br/>
/// find it already there, and every style's request handling is served through the single <see cref="IEndpointTransportServer"/><br/>
/// this feature's component runs — one server, one address, per endpoint.</summary>
///<remarks>Also the home of address announcement, because announcing belongs to the thing that owns the address and its listening<br/>
/// lifecycle: the endpoint announces to every <see cref="IEndpointAddressAnnouncer"/> declared through <see cref="AnnounceAddressTo"/><br/>
/// (reached fluently through the style features' delegating methods, e.g. <c>AddExactlyOnceTessaging().AnnounceAddressTo(...)</c>).</remarks>
public class EndpointTransportServerFeature
{
   readonly List<IEndpointAddressAnnouncer> _addressAnnouncers = [];
   //Typed as the address view, not the component: the feature reads where the server listens; the component's lifetime — including disposal — belongs to the endpoint that runs it.
   IListeningAddressSource? _component;

   ///<summary>Composes the feature into <paramref name="builder"/>'s endpoint, creating it if no other communication style already did.</summary>
   public static EndpointTransportServerFeature GetOrAddTo(IEndpointBuilder builder) => builder.GetOrAddFeature(it => new EndpointTransportServerFeature(it));

   EndpointTransportServerFeature(IEndpointBuilder builder) =>
      builder.AddComponent(resolver =>
      {
         var component = new EndpointTransportServerComponent(resolver.Resolve<IEndpointTransportServer>(),
                                                              _addressAnnouncers,
                                                              resolver.Resolve<EndpointConfiguration>());
         _component = component;
         return component;
      });

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/>. The announcement is made in the<br/>
   /// host's announcing phase — after every endpoint in the host has finished starting to listen and before any endpoint starts<br/>
   /// sending — so an announced address is always one that is actually listening, and a router's first look at a registry sees every<br/>
   /// endpoint the host announced; it is retracted in the mirror phase, before any sending stops, so the address stops being advertised<br/>
   /// before anything goes deaf. An endpoint announces to every announcer declared; declaring none means the endpoint is found some<br/>
   /// other way (a static registry, configuration).</summary>
   public void AnnounceAddressTo(IEndpointAddressAnnouncer announcer) => _addressAnnouncers.Add(announcer);

   ///<summary>The address where the endpoint's transport server listens; null until it is listening.</summary>
   public EndpointAddress? ListeningAddress => _component?.ListeningAddress;
}
