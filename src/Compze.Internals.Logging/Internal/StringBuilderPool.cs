using System.Text;

namespace Compze.Internals.Logging.Internal;

static class StringBuilderPool
{
   const int DefaultCapacity = 256;
   const int MaxPoolableCapacity = 8 * 1024;

   [ThreadStatic] static StringBuilder? _cached;

   public static StringBuilder Rent(int capacityHint)
   {
      var cached = _cached;
      if(cached != null)
      {
         _cached = null;
         cached.Clear();
         return cached;
      }
      return new StringBuilder(Math.Max(capacityHint, DefaultCapacity));
   }

   public static string ToStringAndReturn(StringBuilder builder)
   {
      var result = builder.ToString();
      if(builder.Capacity <= MaxPoolableCapacity)
      {
         _cached = builder;
      }
      return result;
   }
}
