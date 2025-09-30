using System;

namespace Compze.SystemCE.ReactiveCE;

public static class ObservableCE
{
   public static IDisposable Subscribe<TEvent>(this IObservable<TEvent> @this, Action<TEvent> onNext) => @this.Subscribe( new SimpleObserver<TEvent>(onNext: onNext));
}