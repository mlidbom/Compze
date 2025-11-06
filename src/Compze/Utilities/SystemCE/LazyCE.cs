using System;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Utilities.SystemCE;

class LazyCE<TValue>(Func<TValue> factory)
   where TValue : class
{
   readonly ILock _lock = ILock.WithDefaultTimeout();
   TValue? _value;
   readonly Func<TValue> _factory = factory;

   public TValue Value => _lock.DoubleCheckedLocking(() => _value, () => _value = _factory());

   public TValue? ValueIfInitialized() => _value;
}