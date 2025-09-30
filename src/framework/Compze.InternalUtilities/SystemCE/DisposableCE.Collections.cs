using System;
using System.Collections.Generic;
using Compze.SystemCE.LinqCE;

namespace Compze.SystemCE;

static class DisposableCECollections
{
   internal static void DisposeAll(this IEnumerable<IDisposable> disposables) => disposables.ForEach(disposable => disposable.Dispose());
}