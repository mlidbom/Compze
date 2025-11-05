using System;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Utilities.SystemCE;

class LazyCE<TValue>(Func<TValue> factory)
   where TValue : class
{
   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
   TValue? _value;
   readonly Func<TValue> _factory = factory;

   public TValue Value => _monitor.DoubleCheckedLocking(() => _value, () => _value = _factory());

   public TValue? ValueIfInitialized() => _value;
}