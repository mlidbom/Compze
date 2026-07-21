namespace Compze.Threading.Internal.SystemCE;

static class TimeSpanCE
{
   extension(TimeSpan)
   {
      public static TimeSpan Min(TimeSpan first, TimeSpan second) => first < second ? first : second;
   }
}
