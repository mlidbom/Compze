namespace Compze.Threading.Interprocess;

///<summary>A held <see cref="IProcessLock"/>. Disposing it releases the lock, from whichever thread disposes it, so async code may hold it across<br/>
/// awaits. Disposing it again does nothing.</summary>
public interface IProcessLockHold : IDisposable
{
   //review-naming: IProcessLockHold, for what taking the lock returns and disposing releases.
}
