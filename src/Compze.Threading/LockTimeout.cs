using System;
using Compze.Threading.Utilities;
// ReSharper disable UnusedMember.Global

namespace Compze.Threading;

public readonly struct LockTimeout(TimeSpan value) : IEquatable<LockTimeout>
{
   public TimeSpan Value { get; } = value;

   /// <summary>Default lock timeout: 45 seconds under NCrunch (to surface deadlocks before the 60s test timeout), 2 minutes otherwise.</summary>
   public static readonly LockTimeout Default = CompzeEnvironment.IsNCrunch
                                                   ? Seconds(45)
                                                   : Minutes(2);

   public static LockTimeout Milliseconds(long milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static LockTimeout Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static LockTimeout Seconds(long seconds) => new(TimeSpan.FromSeconds(seconds));
   public static LockTimeout Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));
   public static LockTimeout Minutes(long minutes) => new(TimeSpan.FromMinutes(minutes));
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
