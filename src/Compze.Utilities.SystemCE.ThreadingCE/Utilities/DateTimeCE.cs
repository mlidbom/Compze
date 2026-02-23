using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.Utilities;

static class DateTimeCE
{
   public static TimeSpan TimeElapsedSince(DateTime pointInThePast) => DateTime.UtcNow - pointInThePast;
}
