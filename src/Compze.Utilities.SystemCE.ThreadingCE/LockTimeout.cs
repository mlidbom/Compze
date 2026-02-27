using System;

namespace Compze.Utilities.SystemCE.ThreadingCE;

public readonly struct LockTimeout(TimeSpan value) : IEquatable<LockTimeout>
{
   public TimeSpan Value { get; } = value;

   public static LockTimeout Milliseconds(int milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static LockTimeout Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static LockTimeout Seconds(int seconds) => new(TimeSpan.FromSeconds(seconds));
   public static LockTimeout Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));
   public static LockTimeout Minutes(int minutes) => new(TimeSpan.FromMinutes(minutes));
   public static LockTimeout Minutes(double minutes) => new(TimeSpan.FromMinutes(minutes));

   public TimeSpan ToTimeSpan() => Value;
   public static implicit operator TimeSpan(LockTimeout timeout) => timeout.Value;

   public bool Equals(LockTimeout other) => Value.Equals(other.Value);
   public override bool Equals(object? obj) => obj is LockTimeout other && Equals(other);
   public override int GetHashCode() => Value.GetHashCode();
   public static bool operator ==(LockTimeout left, LockTimeout right) => left.Equals(right);
   public static bool operator !=(LockTimeout left, LockTimeout right) => !left.Equals(right);

   public override string ToString() => Value.ToString();
}
