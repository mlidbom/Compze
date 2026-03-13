namespace Compze.Threading.Specifications.TestInfrastructure;

partial class IAwaitableProcessSharedMatrixAttribute
{
   public enum Implementation
   {
      GlobalPollingMutex,
      LocalPollingMutex,
      GlobalSignalingMutex,
      LocalSignalingMutex,
      InterprocessObjectMemoryMapped,
      InterprocessObjectFileBacked
   }
}
