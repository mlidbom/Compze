using System;
using Compze.Threading.ResourceAccess;

namespace Compze.Utilities.SystemCE;

class OptimizedLazy<TValue>(Func<TValue> factory)
   where TValue : class
{
   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
   TValue? _value;
   readonly Func<TValue> _factory = factory;

   public TValue Value => _value ?? _monitor.Update(() => _value ??= _factory());

   public TValue? ValueIfInitialized() => _value;
}