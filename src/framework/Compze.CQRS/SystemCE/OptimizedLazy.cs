using System;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.SystemCE;

class OptimizedLazy<TValue>(Func<TValue> factory)
   where TValue : class
{
   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
   TValue? _value;
   readonly Func<TValue> _factory = factory;

   public TValue Value => _value ?? _monitor.Update(() => _value ??= _factory());

   public bool IsInitialized => _monitor.Read(() => _value != null);
   public TValue? ValueIfInitialized() => _value;
}