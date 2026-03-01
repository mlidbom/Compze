using System;
using Compze.Threading.ResourceAccess;

namespace Compze.Utilities.SystemCE;

public class LazyCE<TValue>(Func<TValue> factory)
   where TValue : class
{
   readonly IMonitor _monitor = IMonitor.WithDefaultTimeout();
   TValue? _value;
   readonly Func<TValue> _factory = factory;

   public TValue Value => _monitor.DoubleCheckedLocking(() => _value, () => _value = _factory());

   public TValue? ValueIfInitialized() => _value;

   public void Reset() => _value = null;
}
