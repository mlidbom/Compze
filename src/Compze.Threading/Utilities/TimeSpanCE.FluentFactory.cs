using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.Utilities;

/// <summary>A collection of extensions to work with timespans</summary>
static partial class TimeSpanCE
{
   /// <summary>Returns a TimeSpan <paramref name="this"/> milliseconds long.</summary>
   public static TimeSpan Milliseconds(this int @this) => TimeSpan.FromMilliseconds(@this);

   /// <summary>Returns a TimeSpan <paramref name="this"/> seconds long.</summary>
   public static TimeSpan Seconds(this int @this) => TimeSpan.FromSeconds(@this);

   /// <summary>Returns a TimeSpan <paramref name="this"/> minutes long.</summary>
   public static TimeSpan Minutes(this int @this) => TimeSpan.FromMinutes(@this);
}
