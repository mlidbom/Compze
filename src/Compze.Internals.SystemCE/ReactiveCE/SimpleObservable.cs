using System.Collections.Immutable;
using Compze.Threading;
using Compze.Contracts;

namespace Compze.Internals.SystemCE.ReactiveCE;

///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling <see cref="OnNext"/> on them all at once.</summary>
///<remarks>
/// Subscribers are called with no lock held. So a subscriber may subscribe or dispose a subscription from inside a notification,<br/>
/// and a thread subscribing or disposing a subscription never waits for another thread's subscribers to finish running.
///</remarks>
///<remarks>
/// Copy-on-write: <see cref="Subscribe"/> and disposing a subscription swap in a new immutable list of subscribers,<br/>
/// while <see cref="OnNext"/> notifies the list that was current when it was called. Consequences a subscriber can observe:<br/>
/// - A subscription disposed while a notification is in progress may still receive that notification.<br/>
/// - A subscription added while a notification is in progress first receives the next one.<br/>
/// - Subscribers are notified in the order they subscribed.<br/>
/// - Subscribing the same observer more than once creates independent subscriptions: it is notified once per subscription,
///   and disposing one subscription leaves the others intact.<br/>
/// - Calls to <see cref="OnNext"/> from different threads are not serialized, so their notifications may reach a subscriber concurrently.
///   Like Rx's <c>Subject&lt;T&gt;</c>, serializing notifications is the publisher's job.
///</remarks>
public class SimpleObservable<TTevent> : IObservable<TTevent>
{
   readonly IMonitor _subscriptionChangeMonitor = IMonitor.New();
   volatile ImmutableList<IObserver<TTevent>> _subscribers = ImmutableList<IObserver<TTevent>>.Empty;

   ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each observer subscribed when this call starts.</summary>
   public void OnNext(TTevent tevent)
   {
      Argument.Assert(tevent is not null);

      _subscribers.ForEach(observer => observer.OnNext(tevent));
   }

   /// <inheritdoc />
   public IDisposable Subscribe(IObserver<TTevent> observer)
   {
      _subscriptionChangeMonitor.Locked(() => _subscribers = _subscribers.Add(observer));
      return new Disposable(() => _subscriptionChangeMonitor.Locked(() => _subscribers = _subscribers.Remove(observer)));
   }
}
