namespace Compze.Teventive.MemoryImageStateStore._internal;

///<summary>The number of a commit to a storage that several memory images share and that numbers its commits in the order they<br/>
/// commit, as a memory image state store's tables do. Each taggregate's stored state carries the number of the commit that last<br/>
/// wrote it, so that a memory image knows which state it holds, and finds what others committed after it by number.</summary>
readonly struct CommitNumber : IEquatable<CommitNumber>, IComparable<CommitNumber>
{
   internal CommitNumber(long value) => Value = value;

   ///<summary>The number before any commit: what a memory image has seen before it has read anything.</summary>
   internal static CommitNumber BeforeTheFirstCommit => new(0);

   internal long Value { get; }

   public bool Equals(CommitNumber other) => Value == other.Value;
   public override bool Equals(object? obj) => obj is CommitNumber other && Equals(other);
   public override int GetHashCode() => Value.GetHashCode();
   public int CompareTo(CommitNumber other) => Value.CompareTo(other.Value);

   public static bool operator ==(CommitNumber left, CommitNumber right) => left.Equals(right);
   public static bool operator !=(CommitNumber left, CommitNumber right) => !left.Equals(right);
   public static bool operator <(CommitNumber left, CommitNumber right) => left.Value < right.Value;
   public static bool operator >(CommitNumber left, CommitNumber right) => left.Value > right.Value;
   public static bool operator <=(CommitNumber left, CommitNumber right) => left.Value <= right.Value;
   public static bool operator >=(CommitNumber left, CommitNumber right) => left.Value >= right.Value;

   public override string ToString() => $"commit {Value}";
}
