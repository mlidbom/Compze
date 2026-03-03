using Compze.Contracts;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace Compze.Threading;

public readonly struct WaitTimeout(TimeSpan value) : IEquatable<WaitTimeout>
{
   public TimeSpan Value { get; } = value;

   public static WaitTimeout Milliseconds(long milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static WaitTimeout Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static WaitTimeout Seconds(long seconds) => new(TimeSpan.FromSeconds(seconds));
   public static WaitTimeout Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));
   public static WaitTimeout Minutes(long minutes) => new(TimeSpan.FromMinutes(minutes));
   public static WaitTimeout Minutes(double minutes) => new(TimeSpan.FromMinutes(minutes));

   public static readonly WaitTimeout Infinite = new(Timeout.InfiniteTimeSpan);

   /// <summary>The default wait timeout: Infinite. Unlike with locks, nothing says that a wait has to complete within any particular time span in general.</summary>
   public static readonly WaitTimeout Default = Infinite;

   public bool IsInfinite => Value == Timeout.InfiniteTimeSpan;

   internal bool IsExpired(DateTime waitStarted) =>
      Argument.Assert(this != Infinite)
              ._(DateTime.UtcNow - waitStarted >= Value);

   internal WaitTimeout TimeRemaining(DateTime waitStarted) =>
      Argument.Assert(this != Infinite)
              ._(new WaitTimeout(TimeSpan.FromTicks(Math.Max(0, (Value - (DateTime.UtcNow - waitStarted)).Ticks))));

   public bool Equals(WaitTimeout other) => Value.Equals(other.Value);
   public override bool Equals(object? obj) => obj is WaitTimeout other && Equals(other);
   public override int GetHashCode() => Value.GetHashCode();
   public static bool operator ==(WaitTimeout left, WaitTimeout right) => left.Equals(right);
   public static bool operator !=(WaitTimeout left, WaitTimeout right) => !left.Equals(right);

   public override string ToString() => Value.ToString();
}
