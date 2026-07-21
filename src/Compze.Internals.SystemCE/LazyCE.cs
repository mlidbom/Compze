using Compze.Threading;

namespace Compze.Internals.SystemCE;

public class LazyCE<TValue>(Func<TValue> factory)
   where TValue : class
{
   readonly IMonitor _monitor = IMonitor.New();
   TValue? _value;
   readonly Func<TValue> _factory = factory;

   public TValue Value => _monitor.DoubleCheckedLocking(ref _value, _factory);

   public TValue? ValueIfInitialized() => _value;
}
