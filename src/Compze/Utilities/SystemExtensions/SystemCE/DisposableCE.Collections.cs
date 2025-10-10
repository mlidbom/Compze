using System;
using System.Collections.Generic;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.SystemCE;

static class DisposableCECollections
{
   internal static void DisposeAll(this IEnumerable<IDisposable> disposables) => disposables.ForEach(disposable => disposable.Dispose());
}