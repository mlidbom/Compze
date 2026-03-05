namespace Compze.Internals.SystemCE;

public static class DisposableCECollections
{
   public static void DisposeAll(this IEnumerable<IDisposable> disposables) => disposables.ForEach(disposable => disposable.Dispose());
}