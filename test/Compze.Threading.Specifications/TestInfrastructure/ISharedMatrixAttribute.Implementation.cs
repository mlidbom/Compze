namespace Compze.Threading.Specifications.TestInfrastructure;

partial class ISharedMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalMutex,
      LocalMutex
   }
}
