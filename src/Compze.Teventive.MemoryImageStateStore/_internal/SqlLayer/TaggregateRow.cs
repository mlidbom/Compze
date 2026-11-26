using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;

///<summary>One row of a memory image state store's taggregate table: a taggregate's id, its class by the name declared for it, and its<br/>
/// snapshot as the consumer's JSON options write it.</summary>
sealed class TaggregateRow
{
   internal TaggregateRow(TaggregateId id, string className, string snapshot)
   {
      Id = id;
      ClassName = className;
      Snapshot = snapshot;
   }

   internal TaggregateId Id { get; }
   internal string ClassName { get; }

   ///<summary>The snapshot's JSON.</summary>
   internal string Snapshot { get; }
}
