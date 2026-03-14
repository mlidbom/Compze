namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;

partial class IAwaitableCriticalSectionMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalMutex,
      LocalMutex
   }
}
