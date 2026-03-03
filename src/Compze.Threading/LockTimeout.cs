using Compze.Threading.Utilities;
using Compze.Contracts;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
// ReSharper disable HeuristicUnreachableCode

namespace Compze.Threading;

public readonly struct LockTimeout : IEquatable<LockTimeout>
{
   public LockTimeout(TimeSpan value) => Argument.Assert(value != Timeout.InfiniteTimeSpan)
                                                 ._(Value = value);

   public TimeSpan Value { get; }

   /// <summary>Default lock timeout: 45 seconds under NCrunch (to surface deadlocks before the 60s test timeout), 2 minutes otherwise.</summary>
#pragma warning disable CS0162
   public static readonly LockTimeout Default = CompzeEnvironment.IsNCrunch
                                                   ? Seconds(45)
                                                   : Minutes(2);
#pragma warning restore CS0162

   public static LockTimeout Milliseconds(long milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static LockTimeout Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   public static LockTimeout Seconds(long seconds) => new(TimeSpan.FromSeconds(seconds));
   public static LockTimeout Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));
   public static LockTimeout Minutes(long minutes) => new(TimeSpan.FromMinutes(minutes));
   public static LockTimeout Minutes(double minutes) => new(TimeSpan.FromMinutes(minutes));

   public bool Equals(LockTimeout other) => Value.Equals(other.Value);
   public override bool Equals(object? obj) => obj is LockTimeout other && Equals(other);
   public override int GetHashCode() => Value.GetHashCode();
   public static bool operator ==(LockTimeout left, LockTimeout right) => left.Equals(right);
   public static bool operator !=(LockTimeout left, LockTimeout right) => !left.Equals(right);

   public override string ToString() => Value.ToString();
}
