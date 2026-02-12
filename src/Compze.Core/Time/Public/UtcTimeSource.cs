using System;
using System.Threading;
using Compze.Core.Time.Testing.Public;

namespace Compze.Core.Time.Public;

public static class UtcTimeSource
{
   static readonly IUtcTimeTimeSource TimeSource = DateTimeNowTimeSource.Instance;

   public static DateTime UtcNow => Override?.Value?.UtcNow ?? TimeSource.UtcNow;

   public static readonly ThreadLocal<IUtcTimeTimeSource?> Override = new();
   public static TestingTimeSourceAdapter Test => TestingTimeSourceAdapter.Instance;
}
