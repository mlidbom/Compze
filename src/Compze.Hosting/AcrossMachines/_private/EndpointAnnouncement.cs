using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Hosting.AcrossMachines._private;

///<summary>What an endpoint announces in a registry: that the endpoint identified by <see cref="EndpointId"/> listens at<br/>
/// <see cref="Address"/>.</summary>
sealed class EndpointAnnouncement
{
   internal EndpointAnnouncement(EndpointId endpointId, EndpointAddress address)
   {
      EndpointId = endpointId;
      Address = address;
   }

   internal EndpointId EndpointId { get; }
   internal EndpointAddress Address { get; }
}
