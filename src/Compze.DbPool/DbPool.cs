using Compze.Contracts;
using Compze.DbPool.SystemCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;

#pragma warning disable CA1724 //I don't care that the class uses the same name as the namespace

namespace Compze.DbPool;

public static class DbPoolRegistrar
{
   public static IComponentRegistrar DbPool(this IComponentRegistrar registrar) =>
      Compze.DbPool.DbPool.RegisterWith(registrar, System.Diagnostics.Debugger.IsAttached ? 10.Minutes() : 65.Seconds());
}

public class DbPool : StrictlyManagedResourceBase<DbPool>
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar, TimeSpan reservationLength) =>
      registrar.Register(Singleton.For<DbPool>()
                                  .CreatedBy((IDbPoolSqlLayer sqlLayer) => new DbPool(sqlLayer, reservationLength))
                                  .DelegateToParentServiceLocatorWhenCloning());

   readonly IDbPoolSqlLayer _sqlLayer;
   readonly DbPoolMachineWideState _machineWideState;
   static TimeSpan _reservationLength;
   internal const int NumberOfDatabases = 50;

   DbPool(IDbPoolSqlLayer sqlLayer, TimeSpan reservationLength) : base(forceStackTraceAllocation: false)
   {
      _sqlLayer = sqlLayer;
      _reservationLength = reservationLength;

      _machineWideState = new DbPoolMachineWideState(sqlLayer.GetType().GetFullNameCompilable());
   }

   readonly IMonitor _lock = IMonitor.New(LockTimeout.Seconds(30));
   readonly Guid _poolId = Guid.NewGuid();
   IReadOnlyList<DbPoolDatabase> _transientCache = new List<DbPoolDatabase>();

   static ILogger _log = CompzeLogger.For<DbPool>();

   public void SetLogLevel(LogLevel logLevel) => _lock.Locked(() => _log = _log.WithLogLevel(logLevel));

   public string ConnectionStringFor(string reservationName) => _lock.Locked(() =>
   {
      Contract.State.NotDisposed(Disposed, this);

      var reservedDatabase = _transientCache.SingleOrDefault(db => db.ReservationName == reservationName);
      if(reservedDatabase != null)
      {
         _log.Debug($"Retrieved reserved pool database from cache: {reservedDatabase.Id}");
         return _sqlLayer.ConnectionStringFor(reservedDatabase);
      }

      reservedDatabase = _machineWideState.ReserveDatabase(reservationName, _poolId, _reservationLength);
      _log.Info($"Reserved pool database: {reservedDatabase.Name}");
      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _transientCache, reservedDatabase);

      try
      {
         _log.Debug($"Resetting database {reservedDatabase.Name}");
         TransactionScopeCe.SuppressAmbient(() => _sqlLayer.ResetDatabase(reservedDatabase));
      }
#pragma warning disable CA1031 //It's hard to know what kind of exception a sql layer may end up throwing, and it's likewise hard to see any other action to take than nuking the database and starting over
      catch(Exception exception)
      {
#pragma warning restore CA1031
         _log.Warning(exception, $"Resetting database {reservedDatabase.Name} failed. Calling {nameof(IDbPoolSqlLayer.EnsureDatabaseExistsAndIsEmpty)}");
         TransactionScopeCe.SuppressAmbient(() => _sqlLayer.EnsureDatabaseExistsAndIsEmpty(reservedDatabase));
      }

      return _sqlLayer.ConnectionStringFor(reservedDatabase);
   });

   public override void Dispose() => _lock.Locked(() =>
   {
      if(Disposed) return;
      base.Dispose();
      _sqlLayer.Dispose(_transientCache);
      _machineWideState.ReleaseReservationsFor(_poolId);
   });
}
