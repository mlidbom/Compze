using System;
using System.Globalization;

// ReSharper disable UnusedMember.Global
namespace Compze.Utilities.SystemCE;

/// <summary>A collection of extensions to work with timespans</summary>
static partial class TimeSpanCE
{
   public static TimeSpan MultiplyBy(this TimeSpan @this, double times) => TimeSpan.FromTicks((long)(@this.Ticks * times));

   public static TimeSpan DivideBy(this TimeSpan @this, double divideBy) => TimeSpan.FromTicks((long)(@this.Ticks / divideBy));

   internal static string ToStringInvariant(this TimeSpan @this, string format) => @this.ToString(format, CultureInfo.InvariantCulture);

   internal static string FormatReadable(this TimeSpan? time) => time == null ? "" : time.Value.FormatReadable();

   internal static string FormatReadable(this TimeSpan time)
   {
      if(time >= OneMillisecond)
      {
         var defaultFormattedWith7SecondDecimalPoints = time.ToStringInvariant(@"ss\.fffffff");

         var parts = defaultFormattedWith7SecondDecimalPoints.Split('.');
         var (integer, decimalPart) = (parts[0], parts[1]);

         // ReSharper disable once ReplaceSubstringWithRangeIndexer
         var d1 = decimalPart.Substring(0, 3);
         var d2 = decimalPart.Substring(3, 3);
         var d3 = decimalPart.Substring(6, 1);

         return $"{integer}.{d1}_{d2}_{d3}";
      }

      return time >= OneMicrosecond ? $"{time.TotalMicroseconds()} microseconds" : $"{time.TotalNanoseconds()} nanoseconds";
   }
}
