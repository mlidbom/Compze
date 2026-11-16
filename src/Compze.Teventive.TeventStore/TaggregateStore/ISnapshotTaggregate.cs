namespace Compze.Teventive.TaggregateStore;

///<summary>A taggregate that a taggregate store keeps as its state, not as its history: it takes a snapshot of its state, of a<br/>
/// serializable type its own class declares, and that snapshot restores it.</summary>
public interface ISnapshotTaggregate : ITaggregate
{
   ///<summary>A snapshot of this taggregate's whole state, from which <see cref="ITaggregateSnapshot.Restore"/> builds an equal instance.</summary>
   ITaggregateSnapshot TakeSnapshot();
}
