using System.Collections.Immutable;
using Compze.Contracts;

namespace Compze.Internals.SystemCE.ReactiveCE;

///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling <see cref="OnNext"/> on them all at once.</summary>
///<remarks>
/// It takes no lock. So a subscriber may subscribe or dispose a subscription from inside a notification,<br/>
/// a thread subscribing or disposing a subscription never waits for another thread,<br/>
/// and an instance nobody has subscribed to is a single small object, cheap enough to give each of a great many objects one.
///</remarks>
///<remarks>
/// Copy-on-write: <see cref="Subscribe"/> and disposing a subscription swap in a new immutable array of subscribers,<br/>
/// trying again when another thread swapped in one of its own first, while <see cref="OnNext"/> notifies the array that was current when it was called.<br/>
/// Consequences a subscriber can observe:<br/>
/// - A subscription disposed while a notification is in progress may still receive that notification.<br/>
/// - A subscription added while a notification is in progress first receives the next one.<br/>
/// - Subscribers are notified in the order they subscribed.<br/>
/// - Subscribing the same observer more than once creates independent subscriptions: it is notified once per subscription,
///   and disposing one subscription leaves the others intact.<br/>
/// - Calls to <see cref="OnNext"/> from different threads are not serialized, so their notifications may reach a subscriber concurrently.
///   Like Rx's <c>Subject&lt;T&gt;</c>, serializing notifications is the publisher's job.<br/>
/// Each change copies the whole array, which suits the few subscribers an observable usually has.
///</remarks>
public class SimpleObservable<TTevent> : IObservable<TTevent>
{
   ImmutableArray<IObserver<TTevent>> _subscribers = [];

   ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each observer subscribed when this call starts.</summary>
   public void OnNext(TTevent tevent)
   {
      Argument.Assert(tevent is not null);

      foreach(var observer in _subscribers)
      {
         observer.OnNext(tevent);
      }
   }

   /// <inheritdoc />
   public IDisposable Subscribe(IObserver<TTevent> observer)
   {
      ImmutableInterlocked.Update(ref _subscribers, static (subscribers, subscribingObserver) => subscribers.Add(subscribingObserver), observer);
      return new Disposable(() => ImmutableInterlocked.Update(ref _subscribers, static (subscribers, unsubscribingObserver) => subscribers.Remove(unsubscribingObserver), observer));
   }
}
