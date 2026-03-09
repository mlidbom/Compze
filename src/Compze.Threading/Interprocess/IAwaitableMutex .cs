namespace Compze.Threading.Interprocess;

public interface IAwaitableMutex : IAwaitableLock, IDisposable
{
   bool IsGlobal { get; }
   string Name { get; }
}
