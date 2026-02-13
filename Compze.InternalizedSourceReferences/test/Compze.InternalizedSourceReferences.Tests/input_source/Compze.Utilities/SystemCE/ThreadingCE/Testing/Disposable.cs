using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.Testing;

public class Disposable(Action action) : IDisposable
{
   readonly Action _action = action;
   public void Dispose() => _action();
}
