namespace Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;

partial class IAwaitableSharedMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalMutex,
      LocalMutex,
      GlobalInterprocessObject,
      LocalInterprocessObject
   }
}
