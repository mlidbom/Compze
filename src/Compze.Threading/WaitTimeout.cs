using System;

namespace Compze.Threading;

public readonly struct WaitTimeout(TimeSpan value) : IEquatable<WaitTimeout>
{
   public TimeSpan Value { get; } = value;

   public static WaitTimeout Milliseconds(int milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static WaitTimeout Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static WaitTimeout Seconds(int seconds) => new(TimeSpan.FromSeconds(seconds));
   public static WaitTimeout Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));
   public static WaitTimeout Minutes(int minutes) => new(TimeSpan.FromMinutes(minutes));
   public static WaitTimeout Minutes(double minutes) => new(TimeSpan.FromMinutes(minutes));

   public TimeSpan ToTimeSpan() => Value;
   public static implicit operator TimeSpan(WaitTimeout timeout) => timeout.Value;

   public bool Equals(WaitTimeout other) => Value.Equals(other.Value);
   public override bool Equals(object? obj) => obj is WaitTimeout other && Equals(other);
   public override int GetHashCode() => Value.GetHashCode();
   public static bool operator ==(WaitTimeout left, WaitTimeout right) => left.Equals(right);
   public static bool operator !=(WaitTimeout left, WaitTimeout right) => !left.Equals(right);

   public override string ToString() => Value.ToString();
}
