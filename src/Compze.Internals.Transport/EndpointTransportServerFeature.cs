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
/// (reached fluently through the style features' delegating methods, e.g. <c>AddDistributedTessaging().AnnounceAddressTo(...)</c>).</remarks>
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

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/>. The announcement is made once<br/>
   /// every endpoint in the host has finished starting to listen — the host's sending phase — so an announced address is always one<br/>
   /// that is actually listening and fully ready; it is retracted as the first act of the host's stopping, before anything goes deaf.<br/>
   /// An endpoint announces to every announcer declared; declaring none means the endpoint is found some other way (a static registry, configuration).</summary>
   public void AnnounceAddressTo(IEndpointAddressAnnouncer announcer) => _addressAnnouncers.Add(announcer);

   ///<summary>The address where the endpoint's transport server listens; null until it is listening.</summary>
   public EndpointAddress? ListeningAddress => _component?.ListeningAddress;
}
