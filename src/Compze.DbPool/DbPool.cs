using Compze.Contracts;
using Compze.DbPool.SystemCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

#pragma warning disable CA1724 //I don't care that the class uses the same name as the namespace

namespace Compze.DbPool;

public static class DbPoolRegistrar
{
   public static IComponentRegistrar DbPool(this IComponentRegistrar registrar) =>
      //A live pool heartbeats its reservations, so this lease is not a ceiling on how long a test may hold a database - it is
      //only how long after a pool STOPS heartbeating (a crash) before its databases are reclaimed. Generous, so a GC pause or a
      //debugger step never trips a false reclaim; longer still under a debugger, where breakpoints freeze the heartbeat.
      Compze.DbPool.DbPool.RegisterWith(registrar, System.Diagnostics.Debugger.IsAttached ? 10.Minutes() : 2.Minutes());
}

public class DbPool : StrictlyManagedResourceBase<DbPool>
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar, TimeSpan reservationLength) =>
      registrar.Register(Singleton.For<DbPool>()
                                  .DelegateToParentServiceLocatorWhenCloning()
                                  .CreatedBy((IDbPoolSqlLayer sqlLayer) => new DbPool(sqlLayer, reservationLength)));

   readonly IDbPoolSqlLayer _sqlLayer;
   readonly DbPoolMachineWideState _machineWideState;
   readonly TimeSpan _reservationLength;
   readonly TimeSpan _leaseHeartbeatInterval;
   internal const int NumberOfDatabases = 50;

   DbPool(IDbPoolSqlLayer sqlLayer, TimeSpan reservationLength) : base(forceStackTraceAllocation: false)
   {
      _sqlLayer = sqlLayer;
      _reservationLength = reservationLength;
      //Renew at a sixth of the lease: a large margin, so even a heartbeat badly delayed by a starved machine still refreshes
      //the lease well before it expires. A live pool must never have a database reclaimed out from under it.
      _leaseHeartbeatInterval = reservationLength / 6;

      _machineWideState = new DbPoolMachineWideState(sqlLayer.GetType().GetFullNameCompilable());

      //The heartbeat captures only these locals - never 'this' - so the running thread holds the pool through a WeakReference
      //alone. A pool abandoned without disposal must stay collectable, or its finalizer could never report the missing Dispose.
      var poolReference = new WeakReference<DbPool>(this);
      var disposeSignal = _disposing.Token;
      var heartbeatInterval = _leaseHeartbeatInterval;
      _leaseHeartbeatThread = new Thread(() => KeepReservedDatabasesLeasedWhileAlive(poolReference, heartbeatInterval, disposeSignal))
                              {
                                 IsBackground = true,
                                 Name = $"DbPool.LeaseHeartbeat.{_poolId:N}"
                              };
      _leaseHeartbeatThread.Start();
   }

   readonly IThreadShared<List<DbPoolDatabase>> _reservedDatabases = IThreadShared.New(new List<DbPoolDatabase>(), LockTimeout.Seconds(30));
   readonly Guid _poolId = Guid.NewGuid();
   readonly CancellationTokenSource _disposing = new();
   readonly Thread _leaseHeartbeatThread;

   static ILogger _log = CompzeLogger.For<DbPool>();

   public void SetLogLevel(LogLevel logLevel) => _reservedDatabases.Locked(_ => _log = _log.WithLogLevel(logLevel));

   public string ConnectionStringFor(string reservationName) => _reservedDatabases.Locked(reservedDatabases =>
   {
      Contract.State.NotDisposed(Disposed, this);

      var reservedDatabase = reservedDatabases.SingleOrDefault(db => db.ReservationName == reservationName);
      if(reservedDatabase != null)
      {
         _log.Debug($"Retrieved reserved pool database from cache: {reservedDatabase.Id}");
         return _sqlLayer.ConnectionStringFor(reservedDatabase);
      }

      reservedDatabase = ReserveAndEmptyAFreshDatabase(reservationName);
      reservedDatabases.Add(reservedDatabase);
      return _sqlLayer.ConnectionStringFor(reservedDatabase);
   });

   ///<summary>Reserves a database and empties it, ready for the reservation to use.</summary>
   ///<remarks>A reserved database can still carry an open transaction left by a leaked prior use — emptying it then blocks on the<br/>
   /// held metadata lock (see <see cref="KeepReservedDatabasesLeasedWhileAlive"/>). The sql layer bounds that wait so the reset<br/>
   /// fails fast rather than hanging; this then abandons the database and reserves another, so one stray transaction can never<br/>
   /// gridlock the pool. The abandoned database stays reserved — a released one is the most-recently-used and would be handed back<br/>
   /// on the next reservation — and is freed when this pool disposes (by when the stray transaction has timed out) or by lease<br/>
   /// expiry. Bounded by <see cref="DatabaseResetAttempts"/>, so a reset that keeps failing for a reason other than a lock wait<br/>
   /// still surfaces.<br/>
   /// The inner reset → <see cref="IDbPoolSqlLayer.EnsureDatabaseExistsAndIsEmpty"/> fallback is retained: a first-ever use has no<br/>
   /// database to reset (the MsSql and PostgreSql layers empty an existing one rather than create it), so creating it is the<br/>
   /// recovery there, and it is also a fair fallback for a transient reset failure.</remarks>
   DbPoolDatabase ReserveAndEmptyAFreshDatabase(string reservationName)
   {
      for(var attempt = 1; ; attempt++)
      {
         var reservedDatabase = _machineWideState.ReserveDatabase(reservationName, _poolId, _reservationLength);
         _log.Info($"Reserved pool database: {reservedDatabase.Name}");
         try
         {
            EmptyDatabase(reservedDatabase);
            return reservedDatabase;
         }
#pragma warning disable CA1031 //A sql layer can throw many exception types for a blocked or failed reset; the recovery — a different database — is the same for all, and a persistent failure still surfaces once the attempts run out.
         catch(Exception exception) when(attempt < DatabaseResetAttempts)
         {
#pragma warning restore CA1031
            _log.Warning(exception, $"Emptying reserved database {reservedDatabase.Name} failed (attempt {attempt} of {DatabaseResetAttempts}); a leaked transaction may still hold its metadata lock. Abandoning it and reserving another.");
         }
      }

      void EmptyDatabase(DbPoolDatabase database)
      {
         try
         {
            _log.Debug($"Resetting database {database.Name}");
            TransactionScopeCe.SuppressAmbient(() => _sqlLayer.ResetDatabase(database));
         }
#pragma warning disable CA1031 //It's hard to know what kind of exception a sql layer may end up throwing, and it's likewise hard to see any other action to take than nuking the database and starting over
         catch(Exception exception)
         {
#pragma warning restore CA1031
            _log.Warning(exception, $"Resetting database {database.Name} failed. Calling {nameof(IDbPoolSqlLayer.EnsureDatabaseExistsAndIsEmpty)}");
            TransactionScopeCe.SuppressAmbient(() => _sqlLayer.EnsureDatabaseExistsAndIsEmpty(database));
         }
      }
   }

   const int DatabaseResetAttempts = 5;

   ///<summary>While this pool is alive it heartbeats its reservations, pushing each lease's expiration out (see<br/>
   /// <see cref="DbPoolDatabase.RenewReservation"/>). Without this a fixed lease would expire under any test that runs longer<br/>
   /// than it, and another reservation would reclaim and DROP the database out from under this still-live pool - the metadata<br/>
   /// lock held by the live pool's open transaction then blocks the DROP until a connection timeout. Only a genuinely dead<br/>
   /// process stops heartbeating; its databases are reclaimed once the lease elapses - crash recovery, not conflict.</summary>
   ///<remarks>Runs on its own thread rather than the thread pool: the very failure this guards against (a machine so overloaded<br/>
   /// that work stalls) is when the thread pool starves, and a starved heartbeat is exactly what would false-reclaim a live<br/>
   /// pool's database.<br/>
   /// It takes the pool only through a <see cref="WeakReference{T}"/>, so a pool abandoned WITHOUT disposal stays collectable and<br/>
   /// its finalizer still reports the missing <see cref="Dispose"/> (see <see cref="StrictlyManagedResourceBase{TInheritor}"/>) - a<br/>
   /// strong capture would root it forever, hiding the leak and holding its databases for good. When the pool is collected the<br/>
   /// loop ends and its abandoned reservations lease-expire. Renewal (<see cref="RenewOwnReservations"/>) goes straight against the<br/>
   /// machine-wide state by <see cref="_poolId"/>, taking no pool-local lock, so a reservation in progress - even one blocked in a<br/>
   /// slow database reset - never delays a heartbeat.</remarks>
   static void KeepReservedDatabasesLeasedWhileAlive(WeakReference<DbPool> poolReference, TimeSpan heartbeatInterval, CancellationToken disposeSignal)
   {
      //WaitOne returns true the instant Dispose cancels the token, ending the loop; false when the interval elapses, driving a renewal.
      while(!disposeSignal.WaitHandle.WaitOne(heartbeatInterval))
      {
         //Abandoned without disposal and collected: stop renewing so the reservations lease-expire and are reclaimed.
         if(!poolReference.TryGetTarget(out var pool)) return;
         pool.RenewOwnReservations();
      }
   }

   void RenewOwnReservations() => _machineWideState.RenewReservationsFor(_poolId, _reservationLength);

   public override void Dispose()
   {
      _reservedDatabases.Locked(reservedDatabases =>
      {
         if(Disposed) return;
         base.Dispose();
         _disposing.Cancel(); //Stop the heartbeat's renewals before freeing this pool's reservations below.
         _sqlLayer.Dispose(reservedDatabases);
         _machineWideState.ReleaseReservationsFor(_poolId);
      });
      _leaseHeartbeatThread.JoinCE(_leaseHeartbeatInterval + 5.Seconds());
      _disposing.Dispose();
   }
}
