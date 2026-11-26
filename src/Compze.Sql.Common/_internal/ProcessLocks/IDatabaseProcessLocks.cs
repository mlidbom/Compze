using Compze.Threading;

namespace Compze.Sql.Common._internal.ProcessLocks;

///<summary>The process locks of one database: locks named within the database, each of which one holder at a time holds, whichever<br/>
/// process, on whichever machine, it is in. What a feature takes when its data in the database must be used by one process at a time,<br/>
/// such as an exactly-once endpoint's tables, or a memory image state store's.</summary>
///<remarks>A lock is held by a live holder, never leased for a time: for the database servers, by a database session of its own, which<br/>
/// the server ends when the holder's process dies or its connection breaks; for SQLite, which has no sessions, by an<br/>
/// <c>IProcessLock</c> of Compze.Threading.Interprocess beside the database's file, which the operating system releases when the holder's process dies.<br/>
/// So no pause, however long, loses a live holder its lock, and a dead holder's lock is free at once, with no cleanup.<br/>
/// No lock is reentrant: a second take waits for the first hold like any other, even in the same process.</remarks>
///<remarks>Whoever takes a lock records who holds it, in the database's <see cref="DatabaseProcessLockHolderSchemaStrings.TableName"/> table,<br/>
/// as part of taking it: what <see cref="HolderOf"/> reads for one that is refused. The record is never cleared, so beside a freed lock it names<br/>
/// the last holder: it is read only when a take is refused, which means the lock is held. The lock, not the record, decides.</remarks>
interface IDatabaseProcessLocks
{
   ///<summary>Takes the lock named <paramref name="lockName"/> for <paramref name="purpose"/> as soon as no other holder holds it, recording<br/>
   /// this process as its holder, and returns the hold, whose disposal releases it: or returns null if another holder still holds it when<br/>
   /// <paramref name="waitTimeout"/> has passed. <see cref="WaitTimeout.Zero"/> gives an immediate answer.<br/>
   /// <paramref name="onLostWhileHeld"/> reports the one way a held lock can be lost without being released: the database session<br/>
   /// holding it dying under a live holder, as when the database becomes unreachable from this process. Another process may then take it.</summary>
   ///<remarks>Takes no part in an ambient transaction: the lock is its own act.</remarks>
   IDatabaseProcessLockHold? TryTake(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld);

   ///<summary>As <see cref="TryTake"/>, holding no thread while it waits.</summary>
   Task<IDatabaseProcessLockHold?> TryTakeAsync(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld);

   ///<summary>Who last recorded itself as the holder of the lock named <paramref name="lockName"/>: what it holds it for, which process it<br/>
   /// is, on which machine, and since when, such as <c>for running the endpoint Orders, by process 4242 (Orders.Host) on SERVER7, since<br/>
   /// 2026-11-26 10:15:00 +01:00</c>. Null if no holder has recorded itself yet, as when another took the lock this instant.</summary>
   string? HolderOf(string lockName);

   ///<summary>As <see cref="HolderOf"/>, holding no thread while it reads.</summary>
   Task<string?> HolderOfAsync(string lockName);
}
