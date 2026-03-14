namespace Compze.Threading.Specifications.TestInfrastructure;

partial class IAwaitableSharedMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalMutex,
      LocalMutex,
      GlobalInterprocessObjectMemoryMapped,
      GlobalInterprocessObjectFileBacked,
      LocalInterprocessObjectMemoryMapped,
      LocalInterprocessObjectFileBacked
   }
}
