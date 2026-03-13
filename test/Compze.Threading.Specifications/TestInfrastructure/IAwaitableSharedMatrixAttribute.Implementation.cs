namespace Compze.Threading.Specifications.TestInfrastructure;

partial class IAwaitableSharedMatrixAttribute
{
   public enum Implementation
   {
      Monitor,
      GlobalPollingMutex,
      LocalPollingMutex,
      GlobalSignalingMutex,
      LocalSignalingMutex
   }
}
