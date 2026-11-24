using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;

///<summary>One row of a memory image state store's taggregate table: a taggregate's id, its class by the name declared for it, its<br/>
/// snapshot as the consumer's JSON options write it, or null for a deleted taggregate, and the number of the commit that wrote it.</summary>
sealed class TaggregateRow
{
   internal TaggregateRow(TaggregateId id, string className, string? snapshot, CommitNumber committedAs)
   {
      Id = id;
      ClassName = className;
      Snapshot = snapshot;
      CommittedAs = committedAs;
   }

   internal TaggregateId Id { get; }
   internal string ClassName { get; }

   ///<summary>The snapshot's JSON, or null when the taggregate is deleted.</summary>
   internal string? Snapshot { get; }

   internal CommitNumber CommittedAs { get; }
}
