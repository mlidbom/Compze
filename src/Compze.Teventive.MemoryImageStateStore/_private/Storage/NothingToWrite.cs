namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>The prepared write of a unit of work that writes nothing to the storage, and of every unit of work of a store with no storage: there is<br/>
/// nothing to commit, to discard, nor to wait for.</summary>
sealed class NothingToWrite : IPreparedWrite
{
   ///<summary>The one instance: it holds nothing.</summary>
   internal static readonly NothingToWrite Instance = new();

   NothingToWrite() {}

   public void Commit() {}
   public void Discard() {}
   public void Complete() {}
}
