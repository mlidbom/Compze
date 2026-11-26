namespace Compze.Sql.Common._internal.ProcessLocks;

///<summary>A held lock of <see cref="IDatabaseProcessLocks"/>. Disposing it releases the lock, and returns once the database has released<br/>
/// it, so that the lock's next holder can take it at once. Disposing it again does nothing.</summary>
interface IDatabaseProcessLockHold : IDisposable, IAsyncDisposable;
