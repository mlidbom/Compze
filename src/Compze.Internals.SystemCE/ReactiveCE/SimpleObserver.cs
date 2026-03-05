using System.Runtime.ExceptionServices;

namespace Compze.Internals.SystemCE.ReactiveCE;

class SimpleObserver<TTevent> : IObserver<TTevent>
{
   readonly Action<TTevent> _onNext;
   readonly Action<Exception> _onError;
   readonly Action _onCompleted;

   public SimpleObserver(Action<TTevent>? onNext = null, Action<Exception>? onError = null, Action? onCompleted = null)
   {
      _onNext = onNext ?? (_ => { });
      _onError = onError ?? (exception => ExceptionDispatchInfo.Capture(exception).Throw());
      _onCompleted = onCompleted ?? (() => { });
   }

   public void OnNext(TTevent value) => _onNext(value);
   public void OnError(Exception error) => _onError(error);
   public void OnCompleted() => _onCompleted();
}