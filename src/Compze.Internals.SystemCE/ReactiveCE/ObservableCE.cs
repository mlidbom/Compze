using Compze.Internals.SystemCE.ReactiveCE.Private;

namespace Compze.Internals.SystemCE.ReactiveCE;

public static class ObservableCE
{
   public static IDisposable Subscribe<TTevent>(this IObservable<TTevent> @this, Action<TTevent> onNext) => @this.Subscribe( new SimpleObserver<TTevent>(onNext: onNext));
}