﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Compze.SystemCE;

///<summary>Methods like Sum,Min,Average etc for IEnumerable&lt;TimeSpan&gt;</summary>
static partial class TimeSpanCE
{
   ///<summary>Returns the smallest timespans</summary>
   public static TimeSpan Min(this IEnumerable<TimeSpan> @this) => @this.Min(currentTimeSpan => currentTimeSpan.TotalMilliseconds).Milliseconds();

   ///<summary>Returns the largest timespans</summary>
   public static TimeSpan Max(this IEnumerable<TimeSpan> @this) => @this.Max(currentTimeSpan => currentTimeSpan.TotalMilliseconds).Milliseconds();

   ///<summary>Returns the sum of the timespans</summary>
   public static TimeSpan Sum(this IEnumerable<TimeSpan> @this) => @this.Sum(currentTimeSpan => currentTimeSpan.TotalMilliseconds).Milliseconds();

   ///<summary>Returns the average of the timespans</summary>
   public static TimeSpan Average(this IEnumerable<TimeSpan> @this) => @this.Average(currentTimeSpan => currentTimeSpan.TotalMilliseconds).Milliseconds();
}