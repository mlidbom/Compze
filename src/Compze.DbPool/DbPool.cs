using Compze.Contracts;
using Compze.DbPool.SystemCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.DbPool._private;

#pragma warning disable CA1724 //I don't care that the class uses the same name as the namespace

namespace Compze.DbPool;

public static class DbPoolRegistrar
{
   public static IComponentRegistrar DbPool(this IComponentRegistrar registrar) =>
      Compze.DbPool.DbPool.RegisterWith(registrar, System.Diagnostics.Debugger.IsAttached ? 10.Minutes() : 65.Seconds());
}

public partial class DbPool : StrictlyManagedResourceBase<DbPool>
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar, TimeSpan reservationLength) =>
      registrar.Register(Singleton.For<DbPool>()
                                  .DelegateToParentServiceLocatorWhenCloning()
                                  .CreatedBy((IDbPoolSqlLayer sqlLayer) => new DbPool(sqlLayer, reservationLength)));

   readonly IDbPoolSqlLayer _sqlLayer;
   readonly DbPoolMachineWideState _machineWideState;
   static TimeSpan _reservationLength;

   DbPool(IDbPoolSqlLayer sqlLayer, TimeSpan reservationLength) : base(forceStackTraceAllocation: false)
   {
      _sqlLayer = sqlLayer;
      _reservationLength = reservationLength;

      _machineWideState = new DbPoolMachineWideState(sqlLayer.GetType().GetFullNameCompilable(), sqlLayer.MaximumNumberOfDatabases);
   }

   ///<summary>How long a call waits for another call that is reserving and resetting the database for the same reservation name, and<br/>
   /// <see cref="Dispose"/> for the calls still doing so: longer than a reservation waits for a free database,<br/>
   /// <see cref="DbPoolMachineWideState.PatienceWithNoFreeDatabase"/>, and<br/>
   /// than a reset takes on a server that a test suite keeps busy.</summary>
   static readonly WaitTimeout WaitForAnotherCallsPreparation = new(2.Minutes());

   ///<summary>The pool's lock: held only to read and change the reservations, never while a database is reserved or reset.</summary>
   readonly IAwaitableThreadShared<Reservations> _reservations = IAwaitableThreadShared.New(new Reservations(), LockTimeout.Seconds(30));
   readonly Guid _poolId = Guid.NewGuid();

   static ILogger _log = CompzeLogger.For<DbPool>();

   public void SetLogLevel(LogLevel logLevel) => _reservations.Update(_ => _log = _log.WithLogLevel(logLevel));

   ///<summary>The connection string of the database this pool holds for <paramref name="reservationName"/>: reserved and emptied<br/>
   /// by the first call for the name, the same database on every later call.</summary>
   ///<remarks>The first call for a name reserves and resets its database without holding the pool's lock, so calls for other names<br/>
   /// don't wait for it: a reset can take seconds on a busy server. Calls for the same name wait until its database is ready.</remarks>
   public string ConnectionStringFor(string reservationName)
   {
      var (reservation, thisCallPreparesIt) = _reservations.UpdateWhen(
         reservations => !reservations.IsBeingPrepared(reservationName),
         reservations =>
         {
            Contract.State.NotDisposed(Disposed, this);
            var reservation = reservations.For(reservationName);
            return (reservation, reservation.TryBeginPreparation());
         },
         timeout: WaitForAnotherCallsPreparation);

      if(thisCallPreparesIt) Prepare(reservation, reservationName);
      return _sqlLayer.ConnectionStringFor(reservation.ReservedDatabase._assert().NotNull());
   }

   ///<summary>Reserves a database for <paramref name="reservation"/>, unless an earlier call did and only its reset failed, and resets it,<br/>
   /// outside the pool's lock. Whether it succeeds or throws, it ends the preparation, so that the calls waiting for it go on.</summary>
   void Prepare(Reservation reservation, string reservationName)
   {
      var database = reservation.ReservedDatabase;
      var isReset = false;
      try
      {
         database ??= ReserveDatabase(reservationName);
         Reset(database);
         isReset = true;
      }
      finally
      {
         _reservations.Update(_ => reservation.EndPreparation(database, isReset));
      }
   }

   DbPoolDatabase ReserveDatabase(string reservationName)
   {
      var reservedDatabase = _machineWideState.ReserveDatabase(reservationName, _poolId, _reservationLength);
      _log.Info($"Reserved pool database: {reservedDatabase.Name}");
      return reservedDatabase;
   }

   void Reset(DbPoolDatabase database)
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

   ///<summary>Releases the pool's databases for other pools to reserve, once the calls still reserving or resetting one are done. Throws<br/>
   /// if the pool held any of them past the end of its reservation, since another pool may then have reset it while it was still in use.</summary>
   public override void Dispose()
   {
      var databasesHeldPastTheirReservation = _reservations.UpdateWhen(
         reservations => !reservations.AnyIsBeingPrepared,
         IReadOnlyList<DbPoolDatabase> (reservations) =>
         {
            if(Disposed) return [];
            base.Dispose();
            var reservedDatabases = reservations.ReservedDatabases;
            _sqlLayer.Dispose(reservedDatabases);
            _machineWideState.ReleaseReservationsFor(_poolId);
            return [..reservedDatabases.Where(db => db.ReservationHasExpired)];
         },
         timeout: WaitForAnotherCallsPreparation);
      _machineWideState.Dispose();

      if(databasesHeldPastTheirReservation.Any())
#pragma warning disable CA1065 // Magnus's decision, 2026-11-27: a pool that held a database past its reservation fails loudly here, at its end, since another pool may have reset the database while it was in use. A log entry would go unread.
         throw new DatabaseHeldPastItsReservationException(databasesHeldPastTheirReservation, _reservationLength, disposedAt: DateTime.UtcNow);
#pragma warning restore CA1065
   }
}
