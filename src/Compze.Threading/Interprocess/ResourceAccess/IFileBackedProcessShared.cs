namespace Compze.Threading.Interprocess.ResourceAccess;

public interface IFileBackedProcessShared<out TShared> : IAwaitableProcessShared<TShared>
{
   void Delete();
}
