namespace Compze.Threading.Specifications.TestInfrastructure;

partial class IAwaitableCriticalSectionMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalMutex,
      LocalMutex
   }
}
