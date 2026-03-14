namespace Compze.Threading.Specifications.ICriticalSection_.Infrastructure;

partial class ICriticalSectionMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalMutex,
      LocalMutex
   }
}
