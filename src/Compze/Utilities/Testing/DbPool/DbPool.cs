using Compze.Utilities.Contracts;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Testing.DbPool.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.ResourceAccess;

namespace Compze.Utilities.Testing.DbPool;

public abstract partial class DbPool : StrictlyManagedResourceBase<DbPool>
{
   protected readonly MachineWideSharedObject<SharedState> MachineWideState;
   static TimeSpan _reservationLength;
   const int NumberOfDatabases = 30;

   protected DbPool() : base(forceStackTraceAllocation:true)
   {
      _reservationLength = System.Diagnostics.Debugger.IsAttached ? TimeSpanCE.Minutes(10) : TimeSpanCE.Seconds(65);

      MachineWideState = MachineWideSharedObject<SharedState>.For(GetType().GetFullNameCompilable().ReplaceInvariant(".", "_"), usePersistentFile: true);
   }

   const string PoolDatabaseNamePrefix = $"Compze_{nameof(DbPool)}_";

   readonly MonitorCE _guard = MonitorCE.WithTimeout(TimeSpanCE.Seconds(30));
   readonly Guid _poolId = Guid.NewGuid();
   protected IReadOnlyList<Database> _transientCache = new List<Database>();

   static ILogger _log = CompzeLogger.For<DbPool>();
   bool _disposed;

   public void SetLogLevel(LogLevel logLevel) => _guard.Update(() => _log = _log.WithLogLevel(logLevel));

   public string ConnectionStringFor(string reservationName) => _guard.Update(() =>
   {
      Assert.State.IsNotDisposed(_disposed, this);

      var reservedDatabase = _transientCache.SingleOrDefault(db => db.ReservationName == reservationName);
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      if(reservedDatabase != null)
      {
         _log.Debug($"Retrieved reserved pool database: {reservedDatabase.Id}");
         return ConnectionStringFor(reservedDatabase);
      }

      var startTime = DateTime.Now;
      var timeoutAt = startTime + TimeSpanCE.Seconds(45);
      while(reservedDatabase == null)
      {
         if(DateTime.Now > timeoutAt) throw new Exception("Timed out waiting for database. Have you missed disposing a database pool? Please check your logs for errors about non-disposed pools.");

         MachineWideState.Update(
            machineWide =>
            {
               if(machineWide.TryReserve(reservationName, _poolId, _reservationLength, out reservedDatabase))
               {
                  _log.Info($"Reserved pool database: {reservedDatabase.Name}");
                  OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _transientCache, reservedDatabase);
               }
            });

         if(reservedDatabase == null)
         {
            Thread.Sleep(10);
         }
      }

      try
      {
         TransactionScopeCe.SuppressAmbient(() => ResetDatabase(reservedDatabase));
      }
      catch(Exception exception)
      {
         _log.Error(exception);
         TransactionScopeCe.SuppressAmbient(() => EnsureDatabaseExistsAndIsEmpty(reservedDatabase));
      }

      return ConnectionStringFor(reservedDatabase);
   });

   protected abstract void ResetDatabase(Database db);

   protected abstract string ConnectionStringFor(Database db);

   protected override void Dispose(bool disposing)
   {
      if(_disposed) return;
      _disposed = true;

      MachineWideState.Update(machineWide => machineWide.ReleaseReservationsFor(_poolId));
      MachineWideState.Dispose();
      base.Dispose(disposing);
   }

   protected abstract void EnsureDatabaseExistsAndIsEmpty(Database db);
}
