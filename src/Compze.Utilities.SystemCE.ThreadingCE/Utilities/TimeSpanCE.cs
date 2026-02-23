using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.Utilities;

/// <summary>A collection of extensions to work with timespans</summary>
static partial class TimeSpanCE
{
   ///<summary>Returns true if the timespan is zero or negative</summary>
   public static bool None(this TimeSpan @this) => @this <= TimeSpan.Zero;
}
