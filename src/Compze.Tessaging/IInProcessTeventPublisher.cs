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
/// <see cref="ITaggregateTevent"/>s a taggregate commits.
///</remarks>
///<remarks>
/// The <see cref="IUnitOfWorkTeventPublisher"/> — the one public way to publish — delegates its in-process delivery
/// here, and additionally routes the tevent through the remote delivery legs its type demands. Subscription
/// is by .NET type compatibility, so a handler subscribed to a base tevent type receives every compatible
/// derived tevent. Every tevent is wrapped in its publisher's
/// <see cref="IPublisherTevent{TTevent}"/> before routing: a subscriber to an inner tevent
/// type receives the tevent unwrapped, and a subscriber to a wrapper type receives the wrapper itself -
/// publisher-conscious subscription.
///</remarks>
interface IInProcessTeventPublisher
{
   ///<summary>Synchronously invokes every in-process handler subscribed to <paramref name="tevent"/>'s type, resolving handler dependencies from <paramref name="unitOfWork"/> — the caller's unit of work, whose transaction the handlers run within.<br/>
   /// A <paramref name="tevent"/> published without a publisher-identifying wrapper is wrapped before routing.</summary>
   void Publish(ITevent tevent, IUnitOfWorkResolver unitOfWork);
}
