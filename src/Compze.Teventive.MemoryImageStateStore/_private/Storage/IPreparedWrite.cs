namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>A write that <see cref="ITaggregateStorage.Prepare"/> has prepared, waiting for its unit of work to complete.</summary>
interface IPreparedWrite
{
   ///<summary>Makes the write. Called when the unit of work commits, and must not fail for any reason that preparing could have found.</summary>
   void Commit();

   ///<summary>Abandons the write. Called when the unit of work rolls back.</summary>
   void Discard();

   ///<summary>Once committed, the number the write committed as, for a storage that numbers its commits; else null.</summary>
   CommitNumber? CommittedAs { get; }
}
