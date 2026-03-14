namespace Compze.Threading.Specifications.TestInfrastructure;

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
