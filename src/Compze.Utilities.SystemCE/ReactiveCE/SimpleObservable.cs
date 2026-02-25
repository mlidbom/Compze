using System;
using System.Collections.Generic;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Contracts;
using static Compze.Contracts.ContractAssertion;

namespace Compze.Utilities.SystemCE.ReactiveCE;

///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling OnNext on them all at once.</summary>
public class SimpleObservable<TTevent> : IObservable<TTevent>
{
   readonly IThreadShared<HashSet<IObserver<TTevent>>> _observerCollection = IThreadShared.WithDefaultTimeouts<HashSet<IObserver<TTevent>>>();

   ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each subscribed observer.</summary>
   public void OnNext(TTevent tevent)
   {
      Argument.Fulfills(tevent is not null);

      _observerCollection.Update(it => it.ForEach(observer => observer.OnNext(tevent)));
   }

   /// <inheritdoc />
   public IDisposable Subscribe(IObserver<TTevent> observer)
   {
      _observerCollection.Update(it =>  it.Add(observer));
      return new Disposable(() => _observerCollection.Update(it => it.Remove(observer)));
   }
}