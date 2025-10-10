using System;

namespace Compze.Utilities.Threading.Testing;

class Disposable(Action action) : IDisposable
{
   readonly Action _action = action;
   public void Dispose() => _action();
}
