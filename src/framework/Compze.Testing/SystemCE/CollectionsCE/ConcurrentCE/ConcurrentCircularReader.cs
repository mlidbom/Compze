using System.Collections.Generic;
using System.Linq;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Testing.SystemCE.CollectionsCE.ConcurrentCE;

static class ConcurrentCircularReader
{
   public static ConcurrentCircularReader<T> ToConcurrentCircularReader<T>(this IEnumerable<T> source) => new(source);
}

class ConcurrentCircularReader<T>(IEnumerable<T> source)
{
   readonly T[] _items = source.ToArray();
   readonly MonitorCE _lock = MonitorCE.WithDefaultTimeout();
   int _current = -1;

   public T Next() => _lock.Update(() =>
   {
      _current++;
      if(_current == _items.Length) _current = 0;
      return _items[_current];
   });
}