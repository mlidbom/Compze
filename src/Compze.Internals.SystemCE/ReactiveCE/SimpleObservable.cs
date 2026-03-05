using Compze.Threading.ResourceAccess;
using Compze.Contracts;

namespace Compze.Internals.SystemCE.ReactiveCE;

///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling OnNext on them all at once.</summary>
public class SimpleObservable<TTevent> : IObservable<TTevent>
{
   readonly IThreadShared<HashSet<IObserver<TTevent>>> _observerCollection = IThreadShared.New(new HashSet<IObserver<TTevent>>());

   ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each subscribed observer.</summary>
   public void OnNext(TTevent tevent)
   {
      Argument.Assert(tevent is not null);

      _observerCollection.Locked(it => it.ForEach(observer => observer.OnNext(tevent)));
   }

   /// <inheritdoc />
   public IDisposable Subscribe(IObserver<TTevent> observer)
   {
      _observerCollection.Locked(it =>  it.Add(observer));
      return new Disposable(() => _observerCollection.Locked(it => it.Remove(observer)));
   }
}