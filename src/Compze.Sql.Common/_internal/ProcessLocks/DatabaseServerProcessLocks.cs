using System.Diagnostics;
using System.Globalization;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.Common._private.ProcessLocks;
using Compze.Threading;
using Holders = Compze.Sql.Common._internal.ProcessLocks.DatabaseProcessLockHolderSchemaStrings;

namespace Compze.Sql.Common._internal.ProcessLocks;

///<summary>The <see cref="IDatabaseProcessLocks"/> of a database on a server, SQL Server, PostgreSQL or MySQL: each lock is a session-scoped lock<br/>
/// of the server's own, held by a database session that the hold keeps open (<see cref="DatabaseProcessLockSession"/>). What each engine does<br/>
/// its own way, its subclass says: the session it opens, and the commands that try to take a lock, release it and record its holder.</summary>
///<remarks>A take that finds the lock held tries again every <see cref="PollInterval"/> on the same session, until it takes it or its wait has<br/>
/// passed, so a wait isn't fair: whoever tries first after a release takes the lock.</remarks>
abstract class DatabaseServerProcessLocks : IDatabaseProcessLocks
{
   static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

   ///<summary>A connection to the database whose session nothing shares: unpooled, since a pooled connection's session outlives its disposal,<br/>
   /// holding its session-scoped locks until the pool reuses or prunes it, and enlisting in no transaction, since the lock is its own act.</summary>
   protected abstract DbConnection NewSessionOfItsOwn();

   ///<summary>Creates the holder table, <see cref="Holders.TableName"/>, unless it exists: the engine's schema manager does, once.</summary>
   protected abstract void EnsureTheHolderTableExists();

   ///<summary>Sets up <paramref name="command"/>, on the session that is to hold the lock, to try to take the lock named<br/>
   /// <paramref name="lockName"/> without waiting: its scalar is 1 when it took the lock, and 0 when another session holds it.</summary>
   protected abstract void SetUpTheTry(DbCommand command, string lockName);

   ///<summary>Sets up <paramref name="command"/>, on the session that holds it, to release the lock named <paramref name="lockName"/>.</summary>
   protected abstract void SetUpTheRelease(DbCommand command, string lockName);

   ///<summary>The command that records <c>@Holder</c> as the holder of the lock named <c>@LockName</c> in the holder table, adding its row or<br/>
   /// changing it.</summary>
   protected abstract string RecordTheHolderSql { get; }

   static string ReadTheHolderSql => $"SELECT {Holders.Holder} FROM {Holders.TableName} WHERE {Holders.LockName} = @{Holders.LockName}";

   public IDatabaseProcessLockHold? TryTake(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld)
   {
      DatabaseProcessLockHolder.AssertIsALockName(lockName);
      var holder = DatabaseProcessLockHolder.ThisProcessTakingItFor(purpose);
      EnsureTheHolderTableExists();
      var session = NewSessionOfItsOwn();
      var isHandedOut = false;
      try
      {
         session.Open();
         var waited = Stopwatch.StartNew();
         while(!TriedToTake())
         {
            if(TimeLeftToWait(waited, waitTimeout) is not {} timeLeft) return null;
            Thread.Sleep(timeLeft < PollInterval ? timeLeft : PollInterval);
         }

         //On the session that now holds the lock, before the hold is handed out: for whoever is refused the lock afterwards, the lock and
         //its recorded holder are one fact.
         using(var record = RecordTheHolderCommand(session, lockName, holder)) record.ExecuteNonQuery();
         isHandedOut = true; //The hold owns the session now: it holds the lock by holding the session.
         return new DatabaseProcessLockSession(session, release => SetUpTheRelease(release, lockName), onLostWhileHeld);
      }
      finally
      {
         if(!isHandedOut) session.Dispose();
      }

      bool TriedToTake()
      {
         using var attempt = session.CreateCommand();
         SetUpTheTry(attempt, lockName);
         return WasTaken(attempt.ExecuteScalar(), lockName);
      }
   }

   public async Task<IDatabaseProcessLockHold?> TryTakeAsync(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld)
   {
      DatabaseProcessLockHolder.AssertIsALockName(lockName);
      var holder = DatabaseProcessLockHolder.ThisProcessTakingItFor(purpose);
      EnsureTheHolderTableExists();
      var session = NewSessionOfItsOwn();
      var isHandedOut = false;
      try
      {
         await session.OpenAsync().caf();
         var waited = Stopwatch.StartNew();
         while(!await TriedToTakeAsync().caf())
         {
            if(TimeLeftToWait(waited, waitTimeout) is not {} timeLeft) return null;
            await Task.Delay(timeLeft < PollInterval ? timeLeft : PollInterval).caf();
         }

         //On the session that now holds the lock, before the hold is handed out: for whoever is refused the lock afterwards, the lock and
         //its recorded holder are one fact.
         var record = RecordTheHolderCommand(session, lockName, holder);
         await using(record.caf()) await record.ExecuteNonQueryAsync().caf();
         isHandedOut = true; //The hold owns the session now: it holds the lock by holding the session.
         return new DatabaseProcessLockSession(session, release => SetUpTheRelease(release, lockName), onLostWhileHeld);
      }
      finally
      {
         if(!isHandedOut) await session.DisposeAsync().caf();
      }

      async Task<bool> TriedToTakeAsync()
      {
         var attempt = session.CreateCommand();
         await using(attempt.caf())
         {
            SetUpTheTry(attempt, lockName);
            return WasTaken(await attempt.ExecuteScalarAsync().caf(), lockName);
         }
      }
   }

   public string? HolderOf(string lockName)
   {
      EnsureTheHolderTableExists();
      using var session = NewSessionOfItsOwn();
      session.Open();
      using var read = session.CreateCommand();
      read.CommandText = ReadTheHolderSql;
      AddParameter(read, Holders.LockName, lockName);
      return read.ExecuteScalar() as string;
   }

   public async Task<string?> HolderOfAsync(string lockName)
   {
      EnsureTheHolderTableExists();
      var session = NewSessionOfItsOwn();
      await using(session.caf())
      {
         await session.OpenAsync().caf();
         var read = session.CreateCommand();
         await using(read.caf())
         {
            read.CommandText = ReadTheHolderSql;
            AddParameter(read, Holders.LockName, lockName);
            return await read.ExecuteScalarAsync().caf() as string;
         }
      }
   }

   DbCommand RecordTheHolderCommand(DbConnection session, string lockName, string holder)
   {
      var command = session.CreateCommand();
      command.CommandText = RecordTheHolderSql;
      AddParameter(command, Holders.LockName, lockName);
      AddParameter(command, Holders.Holder, holder);
      return command;
   }

   ///<summary>Adds the parameter <paramref name="name"/> to <paramref name="command"/>, with the value <paramref name="value"/>.</summary>
   protected static void AddParameter(DbCommand command, string name, object value)
   {
      var parameter = command.CreateParameter();
      parameter.ParameterName = name;
      parameter.Value = value;
      command.Parameters.Add(parameter);
   }

   ///<summary>Whether <paramref name="answer"/>, the scalar of a try, says it took the lock: 1, or 0 for a lock another session holds. Any<br/>
   /// other answer means the server failed to even try, and a refusal naming another holder would lie, so it fails as what it is.</summary>
   static bool WasTaken(object? answer, string lockName)
   {
      return answer is null or DBNull
                ? throw FailedToAnswer()
                : Convert.ToInt64(answer, CultureInfo.InvariantCulture) switch
                {
                   1 => true,
                   0 => false,
                   _ => throw FailedToAnswer()
                };

      InvalidOperationException FailedToAnswer() => new($"Trying to take the database process lock \"{lockName}\" failed instead of answering: it returned {answer ?? "null"}.");
   }

   ///<summary>How much longer a take that began <paramref name="waited"/> ago may wait, at most a <see cref="PollInterval"/> for an infinite<br/>
   /// <paramref name="waitTimeout"/>: null when it has waited all it may.</summary>
   static TimeSpan? TimeLeftToWait(Stopwatch waited, WaitTimeout waitTimeout)
   {
      if(waitTimeout.IsInfinite) return PollInterval;
      var timeLeft = waitTimeout.Value - waited.Elapsed;
      return timeLeft > TimeSpan.Zero ? timeLeft : null;
   }
}
