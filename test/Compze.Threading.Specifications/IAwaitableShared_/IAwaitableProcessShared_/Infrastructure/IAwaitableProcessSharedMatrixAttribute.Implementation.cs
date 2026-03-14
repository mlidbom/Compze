namespace Compze.Threading.Specifications.IAwaitableShared_.IAwaitableProcessShared_.Infrastructure;

partial class IAwaitableProcessSharedMatrixAttribute
{
   public enum Implementation
   {
      GlobalMutex,
      LocalMutex,
      GlobalInterprocessObject,
      LocalInterprocessObject
   }
}
