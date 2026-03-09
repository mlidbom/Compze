using Compze.Threading;

namespace Compze.Internals.SystemCE;

public class LazyCE<TValue>(Func<TValue> factory)
   where TValue : class
{
   readonly IMonitor _lock = IMonitor.New();
   TValue? _value;
   readonly Func<TValue> _factory = factory;

   public TValue Value => _lock.DoubleCheckedLocking(() => _value, () => _value = _factory());

   public TValue? ValueIfInitialized() => _value;

   public void Reset() => _value = null;
}
