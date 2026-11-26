namespace Compze.Threading.Interprocess.ResourceAccess;

///<summary>A strongly-typed object shared across processes. Changes made by one process are immediately visible to all others.
///<para>The object is persisted to disk and survives process restarts and reboots. All reads and updates are atomic and protected by a cross-process mutex.</para>
///</summary>
public interface IInterprocessObject<out T> : IAwaitableProcessShared<T> where T : class
{
   //review-naming: Compze.Threading.Interprocess.ResourceAccess, the namespace IInterprocessObject and its serializers came to from
   //   Compze.InterprocessObject: beside IAwaitableProcessShared<T>, which it extends, as Compze.Threading.ResourceAccess holds IShared<T>.

   ///<summary>Deletes the backing file from disk, destroying the shared state.</summary>
   void Delete();
}
