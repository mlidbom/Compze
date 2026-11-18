namespace Compze.Teventive.Taggregates._private;

///<summary>A copy of a taggregate made through a snapshot doesn't hold the state the taggregate holds:<br/>
/// what <see cref="SnapshotRoundTrip"/> found, with each difference on a line of its own.</summary>
class SnapshotRoundTripFailedException(Type taggregateClass, IReadOnlyList<string> differences)
   : Exception($"A copy of a {taggregateClass.FullName} made through a snapshot differs from the original:{Environment.NewLine}{string.Join(Environment.NewLine, differences)}");
