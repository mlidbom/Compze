using Compze.Abstractions;
namespace Compze.Teventive.TaggregateStore;

///<summary>The state of an <see cref="ISnapshotTaggregate"/> at one moment, as a serializable value of a type the taggregate's class<br/>
/// declares, and what restores the taggregate from it. Restoring applies no tevents: the snapshot sets the state directly.</summary>
public interface ITaggregateSnapshot
{
   ///<summary>A new instance of the taggregate, holding the state this snapshot holds, with <paramref name="id"/> and <paramref name="version"/>.</summary>
   ISnapshotTaggregate Restore(TaggregateId id, int version);
}
