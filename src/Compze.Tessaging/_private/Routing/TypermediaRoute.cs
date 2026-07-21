using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging._private.Routing;

///<summary>A live route for a typermedia type: a connected endpoint whose current advertisement handles the type, and the<br/>
/// address its typermedia tessages travel to. What <see cref="ITessagingRouter.TypermediaRoutesFor"/> answers with —<br/>
/// interpreting the count (none, one, several) is the asker's business, not routing's: waiting sends wait for the list to<br/>
/// become a single entry (see <see cref="HandlerAvailability.IHandlerAvailability"/>).</summary>
class TypermediaRoute
{
   ///<summary>The identity of the connected endpoint advertising the handler.</summary>
   internal EndpointId HandlerEndpointId { get; }

   ///<summary>Where the handler endpoint listens — the address the typermedia tessage travels to.</summary>
   internal EndpointAddress Address { get; }

   internal TypermediaRoute(EndpointId handlerEndpointId, EndpointAddress address)
   {
      HandlerEndpointId = handlerEndpointId;
      Address = address;
   }
}
