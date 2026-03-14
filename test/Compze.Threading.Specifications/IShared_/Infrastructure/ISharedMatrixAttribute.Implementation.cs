namespace Compze.Threading.Specifications.IShared_.Infrastructure;

partial class ISharedMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalMutex,
      LocalMutex
   }
}
