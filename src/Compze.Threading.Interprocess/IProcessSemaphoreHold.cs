namespace Compze.Threading.Interprocess;

///<summary>Units of an <see cref="IProcessSemaphore"/>, held. Disposing it releases them, from whichever thread disposes it, so async code may hold<br/>
/// it across awaits. Disposing it again does nothing.</summary>
public interface IProcessSemaphoreHold : IDisposable
{
   //review-naming: IProcessSemaphoreHold, for what taking units returns and disposing releases, as IProcessLockHold is for IProcessLock.

   ///<summary>How many units it holds.</summary>
   int Units { get; }
}
