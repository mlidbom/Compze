using Compze.Abstractions.Time.Testing;

namespace Compze.Abstractions.Time;

public static class UtcTimeSource
{
   static readonly IUtcTimeTimeSource TimeSource = DateTimeNowTimeSource.Instance;

   public static DateTime UtcNow => Override.Value?.UtcNow ?? TimeSource.UtcNow;

   internal static readonly ThreadLocal<IUtcTimeTimeSource?> Override = new();
   public static TestingTimeSourceAdapter Test => TestingTimeSourceAdapter.Instance;
}
