using Compze.Contracts;

namespace Compze.Threading;

public readonly struct PollingInterval : IEquatable<PollingInterval>
{
   PollingInterval(TimeSpan value) => Argument.Assert(value > TimeSpan.Zero)
                                              ._(Value = value);

   TimeSpan Value { get; }

   // ReSharper disable once MemberCanBeInternal
   public static readonly PollingInterval Default = Milliseconds(50);

   public static PollingInterval Milliseconds(long milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static PollingInterval Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static PollingInterval Seconds(long seconds) => new(TimeSpan.FromSeconds(seconds));
   public static PollingInterval Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));

   public bool Equals(PollingInterval other) => Value.Equals(other.Value);
   public override bool Equals(object? obj) => obj is PollingInterval other && Equals(other);
   public override int GetHashCode() => Value.GetHashCode();
   public static bool operator ==(PollingInterval left, PollingInterval right) => left.Equals(right);
   public static bool operator !=(PollingInterval left, PollingInterval right) => !left.Equals(right);

   public static implicit operator TimeSpan(PollingInterval value) => value.Value;
   public TimeSpan ToTimeSpan() => this;

   public override string ToString() => Value.ToString();
}
