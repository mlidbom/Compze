namespace Compze.Threading.Specifications.TestInfrastructure;

partial class ICriticalSectionMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalMutex,
      LocalMutex
   }
}
