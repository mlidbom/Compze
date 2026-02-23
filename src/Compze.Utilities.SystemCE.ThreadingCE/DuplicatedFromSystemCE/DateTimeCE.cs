using System;

namespace Compze.Utilities.SystemCE;

static class DateTimeCE
{
   public static TimeSpan TimeElapsedSince(DateTime pointInThePast) => DateTime.UtcNow - pointInThePast;
}
