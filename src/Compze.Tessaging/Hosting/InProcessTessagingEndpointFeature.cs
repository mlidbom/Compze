using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Implementation;

namespace Compze.Tessaging.Hosting;

///<summary>
/// Declares the endpoint's Tessaging in-process-only: tevents are delivered synchronously, on the publishing
/// thread, within the publisher's transaction, to this process's handlers — and nowhere else. Composes
/// tessage handling (<see cref="TessageHandlingEndpointFeature"/>) with the in-process-only tevent
/// publication mode (<see cref="InProcessOnlyTeventStoreTeventPublisher"/>); wires no transport, inbox,
/// outbox, or tommand scheduler, so the endpoint has no Tessaging runtime lifecycle at all. Created
/// idempotently through <see cref="EndpointBuilderTessagingExtensions.AddInProcessTessaging"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>.
///</summary>
///<remarks>
/// Mutually exclusive with <see cref="DistributedTessagingEndpointFeature"/>: an endpoint declares exactly
/// one tevent publication mode, and declaring both fails loudly at setup time. An endpoint that wants
/// guaranteed, transactional tommand delivery within a single process is not an in-process endpoint — it is a
/// distributed endpoint that happens to be alone in its host.
///</remarks>
public class InProcessTessagingEndpointFeature
{
   public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   internal InProcessTessagingEndpointFeature(IEndpointBuilder builder)
   {
      builder.Registrar.AssertNoTeventPublicationModeIsDeclared()
                       .InProcessOnlyTeventStoreTeventPublisher();
      RegisterHandlers = builder.AddTessageHandling().RegisterHandlers;
   }
}
