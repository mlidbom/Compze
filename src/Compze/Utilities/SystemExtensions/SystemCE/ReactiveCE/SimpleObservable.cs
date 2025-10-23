using System;
using System.Collections.Generic;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.ResourceAccess;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.SystemCE.ReactiveCE;

///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling OnNext on them all at once.</summary>
class SimpleObservable<TEvent> : IObservable<TEvent>
{
   readonly IThreadShared<HashSet<IObserver<TEvent>>> _observerCollection = IThreadShared.WithDefaultTimeout<HashSet<IObserver<TEvent>>>();

   ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each subscribed observer.</summary>
   public void OnNext(TEvent @event)
   {
      Argument.NotNull(@event);

      _observerCollection.Update(it => it.ForEach(observer => observer.OnNext(@event)));
   }

   /// <inheritdoc />
   public IDisposable Subscribe(IObserver<TEvent> observer)
   {
      _observerCollection.Update(it =>  it.Add(observer));
      return DisposableCE.Create(() => _observerCollection.Update(it => it.Remove(observer)));
   }
}