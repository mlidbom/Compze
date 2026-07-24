using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Internals.Testing.Utilities.Awaiting;

///<summary>How long a <see cref="PollAwaitExtensions.PollAwait{TThis}"/> waits between two evaluations of the condition it is
/// waiting for.</summary>
///<remarks>A floor, not a period: sleeping for a duration guarantees only that at least that much time passes, so the actual gap
/// between evaluations depends on the platform's timer resolution and on how loaded the machine is. Nothing is timed by it — it
/// only decides how often the question gets asked — so being imprecise costs nothing but a few wasted evaluations.</remarks>
public readonly struct PollingInterval : IEquatable<PollingInterval>
{
   PollingInterval(TimeSpan value)
   {
      Argument.Assert(value > TimeSpan.Zero);
      Value = value;
   }

   ///<summary>The underlying <see cref="TimeSpan"/> value.</summary>
   public TimeSpan Value { get; }

   ///<summary>The default polling interval: 1 millisecond. Nearly every condition a specification waits for is a field read or a
   /// collection lookup, so asking often is cheap and makes the wait end promptly. A condition that is expensive to evaluate —
   /// one that queries a database, or takes a contended lock — passes its own longer interval instead.</summary>
   public static readonly PollingInterval Default = Milliseconds(1);

   ///<summary>Returns a <see cref="PollingInterval"/> of <paramref name="milliseconds"/> duration.</summary>
   public static PollingInterval Milliseconds(double milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));
   ///<summary>Returns a <see cref="PollingInterval"/> of <paramref name="seconds"/> duration.</summary>
   public static PollingInterval Seconds(double seconds) => new(TimeSpan.FromSeconds(seconds));

   ///<inheritdoc/>
   public bool Equals(PollingInterval other) => Value.Equals(other.Value);
   ///<inheritdoc/>
   public override bool Equals(object? obj) => obj is PollingInterval other && Equals(other);
   ///<inheritdoc/>
   public override int GetHashCode() => Value.GetHashCode();
   ///<summary>Determines whether two <see cref="PollingInterval"/>s represent the same duration.</summary>
   public static bool operator ==(PollingInterval left, PollingInterval right) => left.Equals(right);
   ///<summary>Determines whether two <see cref="PollingInterval"/>s represent different durations.</summary>
   public static bool operator !=(PollingInterval left, PollingInterval right) => !left.Equals(right);

   ///<summary>Implicitly converts to the underlying <see cref="TimeSpan"/> <see cref="Value"/>.</summary>
   public static implicit operator TimeSpan(PollingInterval value) => value.Value;
   ///<summary>Returns the underlying <see cref="TimeSpan"/> <see cref="Value"/>.</summary>
   public TimeSpan ToTimeSpan() => this;

   ///<inheritdoc/>
   public override string ToString() => Value.ToString();
}
