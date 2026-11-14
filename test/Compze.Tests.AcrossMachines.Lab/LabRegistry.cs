using Compze.Hosting.AcrossMachines;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>The lab registry: the registry the lab endpoint and the conversing endpoint both open on the lab's<br/>
/// <see cref="EndpointRegistryServer"/> (<see cref="NetworkEndpointRegistry"/>), and find each other through.</summary>
static class LabRegistry
{
   internal const string Name = "Compze.AcrossMachinesLab";
}
