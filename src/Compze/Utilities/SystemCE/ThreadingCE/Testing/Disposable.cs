using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.Testing;

class Disposable(Action action) : IDisposable
{
   readonly Action _action = action;
   public void Dispose() => _action();
}
