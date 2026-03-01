using System;

namespace Compze.Threading.Utilities;

static class DateTimeCE
{
   public static TimeSpan TimeElapsedSince(DateTime pointInThePast) => DateTime.UtcNow - pointInThePast;
}
