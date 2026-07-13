using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging;

///<summary>
/// Publishes a <see cref="ITevent"/> to the handlers subscribed to it in this process — synchronously, on the
/// calling thread, within the caller's transaction. This is the in-process delivery of a tevent: the leg every
/// tevent travels, whether or not it is also routed to other endpoints.
///</summary>
///<remarks>
/// It is independent of the teventive event store: any <see cref="ITevent"/> may be published, not only the
/// <see cref="ITaggregateTevent"/>s a taggregate commits. This is the seam that lets tevents restructure the
/// internal flow of a single process with no messaging infrastructure at all.
///</remarks>
///<remarks>
/// The event-store publishers delegate their local delivery here; a distributed endpoint additionally enqueues
/// the tevent on its outbox for remote subscribers. Subscription is by .NET type compatibility, so a handler
/// subscribed to a base tevent type receives every compatible derived tevent. Every tevent is wrapped in its
/// publisher's <see cref="IPublisherIdentifyingTevent{TTevent}"/> before routing: a subscriber to an inner tevent
/// type receives the tevent unwrapped, and a subscriber to a wrapper type receives the wrapper itself -
/// publisher-conscious subscription.
///</remarks>
public interface IInProcessTeventPublisher
{
   ///<summary>Synchronously invokes every in-process handler subscribed to <paramref name="tevent"/>'s type, resolving handler dependencies from <paramref name="scopeResolver"/>, within the current transaction.<br/>
   /// A <paramref name="tevent"/> published without a publisher-identifying wrapper is wrapped before routing.</summary>
   void Publish(ITevent tevent, IScopeResolver scopeResolver);
}
