using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tests.Infrastructure.SystemCE.CollectionsCE.ConcurrentCE;

public static class ConcurrentCircularReader
{
   public static ConcurrentCircularReader<T> ToConcurrentCircularReader<T>(this IEnumerable<T> source) => new(source);
}

public class ConcurrentCircularReader<T>(IEnumerable<T> source)
{
   readonly T[] _items = source.ToArray();
   readonly IMonitor _monitor = IMonitor.WithDefaultTimeout();
   int _current = -1;

   public T Next() => _monitor.Locked(() =>
   {
      _current++;
      if(_current == _items.Length) _current = 0;
      return _items[_current];
   });
}