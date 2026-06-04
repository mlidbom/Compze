using Compze.Threading.Utilities;
using Compze.Contracts;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
// ReSharper disable HeuristicUnreachableCode

namespace Compze.Threading;

///<summary>A strongly-typed duration used for lock acquisition timeouts. Unlike <see cref="WaitTimeout"/>, infinite values are not allowed — locks must always have a finite timeout to surface deadlocks.</summary>
public readonly struct LockTimeout : IEquatable<LockTimeout>
{
   ///<summary>Creates a <see cref="LockTimeout"/> with the specified <paramref name="value"/>. Throws if <paramref name="value"/> is <see cref="Timeout.InfiniteTimeSpan"/>.</summary>
   public LockTimeout(TimeSpan value) => Argument.Assert(value != Timeout.InfiniteTimeSpan)
                                                 .__(Value = value);

   ///<summary>The underlying <see cref="TimeSpan"/> value.</summary>
   public TimeSpan Value { get; }

   /// <summary>Default lock timeout: 45 seconds under NCrunch (to surface deadlocks before the 60s test timeout), 2 minutes otherwise.</summary>
#pragma warning disable CS0162
   public static readonly LockTimeout Default = CompzeEnvironment.IsNCrunch
                                                   ? Seconds(45)
                                                   : Minutes(2);
#pragma warning restore CS0162

   ///<summary>Returns a <see cref="LockTimeout"/> of <paramref name="milliseconds"/> duration.</summary>
   public static LockTimeout Milliseconds(long milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   ///<summary>Returns a <see cref="LockTimeout"/> of <paramref name="milliseconds"/> duration.</summary>
   public static LockTimeout Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   ///<summary>Returns a <see cref="LockTimeout"/> of <paramref name="seconds"/> duration.</summary>
   public static LockTimeout Seconds(long seconds) => new(TimeSpan.FromSeconds(seconds));
   ///<summary>Returns a <see cref="LockTimeout"/> of <paramref name="seconds"/> duration.</summary>
   public static LockTimeout Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));
   ///<summary>Returns a <see cref="LockTimeout"/> of <paramref name="minutes"/> duration.</summary>
   public static LockTimeout Minutes(long minutes) => new(TimeSpan.FromMinutes(minutes));
   ///<summary>Returns a <see cref="LockTimeout"/> of <paramref name="minutes"/> duration.</summary>
   public static LockTimeout Minutes(double minutes) => new(TimeSpan.FromMinutes(minutes));

   ///<summary>A <see cref="LockTimeout"/> of zero duration. Useful for non-blocking lock attempts.</summary>
   public static LockTimeout Zero => new(TimeSpan.Zero);

   ///<inheritdoc/>
   public bool Equals(LockTimeout other) => Value.Equals(other.Value);
   ///<inheritdoc/>
   public override bool Equals(object? obj) => obj is LockTimeout other && Equals(other);
   ///<inheritdoc/>
   public override int GetHashCode() => Value.GetHashCode();
   ///<summary>Determines whether two <see cref="LockTimeout"/>s represent the same duration.</summary>
   public static bool operator ==(LockTimeout left, LockTimeout right) => left.Equals(right);
   ///<summary>Determines whether two <see cref="LockTimeout"/>s represent different durations.</summary>
   public static bool operator !=(LockTimeout left, LockTimeout right) => !left.Equals(right);

   ///<summary>Implicitly converts to the underlying <see cref="TimeSpan"/> <see cref="Value"/>.</summary>
   public static implicit operator TimeSpan(LockTimeout value) => value.Value;
   ///<summary>Returns the underlying <see cref="TimeSpan"/> <see cref="Value"/>.</summary>
   public TimeSpan ToTimeSpan() => this;

   ///<inheritdoc/>
   public override string ToString() => Value.ToString();
}
