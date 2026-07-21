namespace Compze.Threading._internal.SystemCE;

static class TimeSpanCE
{
   extension(TimeSpan)
   {
      public static TimeSpan Min(TimeSpan first, TimeSpan second) => first < second ? first : second;
   }
}
