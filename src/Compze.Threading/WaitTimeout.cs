using Compze.Contracts;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace Compze.Threading;

///<summary>A strongly-typed duration used for condition-wait timeouts. Supports <see cref="Infinite"/> to wait indefinitely.</summary>
public readonly struct WaitTimeout(TimeSpan value) : IEquatable<WaitTimeout>
{
   ///<summary>The underlying <see cref="TimeSpan"/> value.</summary>
   public TimeSpan Value { get; } = value;

   ///<summary>Returns a <see cref="WaitTimeout"/> of <paramref name="milliseconds"/> duration.</summary>
   public static WaitTimeout Milliseconds(long milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   ///<summary>Returns a <see cref="WaitTimeout"/> of <paramref name="milliseconds"/> duration.</summary>
   public static WaitTimeout Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   ///<summary>Returns a <see cref="WaitTimeout"/> of <paramref name="seconds"/> duration.</summary>
   public static WaitTimeout Seconds(long seconds) => new(TimeSpan.FromSeconds(seconds));
   ///<summary>Returns a <see cref="WaitTimeout"/> of <paramref name="seconds"/> duration.</summary>
   public static WaitTimeout Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));
   ///<summary>Returns a <see cref="WaitTimeout"/> of <paramref name="minutes"/> duration.</summary>
   public static WaitTimeout Minutes(long minutes) => new(TimeSpan.FromMinutes(minutes));
   ///<summary>Returns a <see cref="WaitTimeout"/> of <paramref name="minutes"/> duration.</summary>
   public static WaitTimeout Minutes(double minutes) => new(TimeSpan.FromMinutes(minutes));

   ///<summary>A <see cref="WaitTimeout"/> that never expires.</summary>
   public static readonly WaitTimeout Infinite = new(Timeout.InfiniteTimeSpan);

   /// <summary>The default wait timeout: Infinite. Unlike with locks, nothing says that a wait has to complete within any particular time span in general.</summary>
   public static readonly WaitTimeout Default = Infinite;

   ///<summary>True if this timeout represents an infinite wait.</summary>
   public bool IsInfinite => Value == Timeout.InfiniteTimeSpan;

   internal bool IsExpired(DateTime waitStarted) =>
      Argument.Assert(this != Infinite)
              .__(DateTime.UtcNow - waitStarted >= Value);

   internal WaitTimeout TimeRemaining(DateTime waitStarted) =>
      Argument.Assert(this != Infinite)
              .__(new WaitTimeout(TimeSpan.FromTicks(Math.Max(0, (Value - (DateTime.UtcNow - waitStarted)).Ticks))));

   public bool Equals(WaitTimeout other) => Value.Equals(other.Value);
   public override bool Equals(object? obj) => obj is WaitTimeout other && Equals(other);
   public override int GetHashCode() => Value.GetHashCode();
   public static bool operator ==(WaitTimeout left, WaitTimeout right) => left.Equals(right);
   public static bool operator !=(WaitTimeout left, WaitTimeout right) => !left.Equals(right);

   public static implicit operator TimeSpan(WaitTimeout value) => value.Value;
   public TimeSpan ToTimeSpan() => this;

   public override string ToString() => Value.ToString();
}
