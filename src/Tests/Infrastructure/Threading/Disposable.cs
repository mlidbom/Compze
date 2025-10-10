using System;

namespace Compze.Tests.Infrastructure.Threading;

class Disposable(Action action) : IDisposable
{
   readonly Action _action = action;
   public void Dispose() => _action();
}
