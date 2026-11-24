using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;

///<summary>What a commit writes to the row of a taggregate it adds or changes: its class by the name declared for it, and its snapshot as<br/>
/// the consumer's JSON options write it.</summary>
sealed class TaggregateRowToSave
{
   internal TaggregateRowToSave(TaggregateId id, string className, string snapshot)
   {
      Id = id;
      ClassName = className;
      Snapshot = snapshot;
   }

   internal TaggregateId Id { get; }
   internal string ClassName { get; }
   internal string Snapshot { get; }
}
