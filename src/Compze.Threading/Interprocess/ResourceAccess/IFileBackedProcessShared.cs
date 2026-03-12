namespace Compze.Threading.Interprocess.ResourceAccess;

///<summary>An <see cref="IAwaitableProcessShared{TShared}"/> that persists the shared object to a file on disk. Updates are serialized through a cross-process mutex.</summary>
public interface IFileBackedProcessShared<out TShared> : IAwaitableProcessShared<TShared>
{
   ///<summary>Deletes the backing file from disk.</summary>
   void Delete();
}
