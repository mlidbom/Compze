using Compze.Contracts;

namespace Compze.Threading;

///<summary>A strongly-typed duration representing the interval between polling attempts when waiting for a condition in a polling-based mutex.</summary>
public readonly struct PollingInterval : IEquatable<PollingInterval>
{
   PollingInterval(TimeSpan value) => Argument.Assert(value > TimeSpan.Zero)
                                              .__(Value = value);

   TimeSpan Value { get; }

   ///<summary>The default polling interval: 50 milliseconds.</summary>
   // ReSharper disable once MemberCanBeInternal
   public static readonly PollingInterval Default = Milliseconds(50);

   ///<summary>Returns a <see cref="PollingInterval"/> of <paramref name="milliseconds"/> duration.</summary>
   public static PollingInterval Milliseconds(long milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   ///<summary>Returns a <see cref="PollingInterval"/> of <paramref name="milliseconds"/> duration.</summary>
   public static PollingInterval Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   ///<summary>Returns a <see cref="PollingInterval"/> of <paramref name="seconds"/> duration.</summary>
   public static PollingInterval Seconds(long seconds) => new(TimeSpan.FromSeconds(seconds));
   ///<summary>Returns a <see cref="PollingInterval"/> of <paramref name="seconds"/> duration.</summary>
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
