using System.Globalization;

namespace Compze.DbPool._private;

///<summary>Thrown by <see cref="DbPool.ConnectionStringFor"/> when no database was free for a reservation for as long as<br/>
/// <see cref="DbPoolMachineWideState.PatienceWithNoFreeDatabase"/>, and no more could be created. It says how many databases were reserved,<br/>
/// and by how many pools, when the wait ended.</summary>
///<remarks>A pool that waits for a database keeps the ones it already holds. So when the pools that hold every database each wait for one more,<br/>
/// none is released until their waits time out: a deadlock that more databases make rarer but no number of them rules out.</remarks>
sealed class TimedOutWaitingForAFreeDatabaseException : Exception
{
   internal TimedOutWaitingForAFreeDatabaseException(DbPoolState state, int maximumNumberOfDatabases, TimeSpan waited)
      : base(MessageFor(state, maximumNumberOfDatabases, waited)) {}

   static string MessageFor(DbPoolState state, int maximumNumberOfDatabases, TimeSpan waited)
   {
      var reserved = state.Databases.Where(db => db.IsReserved).ToList();
      var pools = reserved.Select(db => db.ReservedByPoolId).Distinct().Count();
      var processes = reserved.Select(db => db.ReservedInProcess).Distinct().Count();
      return $"""
              Timed out after {waited.TotalSeconds.ToString(CultureInfo.InvariantCulture)} seconds waiting for a free pool database: {reserved.Count} of the {state.Databases.Count} databases were reserved, by {pools} {(pools == 1 ? "pool" : "pools")} in {processes} {(processes == 1 ? "process" : "processes")}, and the SQL layer allows no more than {maximumNumberOfDatabases}.
              A pool that waits for a database keeps the ones it holds. So when pools that each wait for one more hold every database, none is released until their waits time out, however many databases there are. That happens when more tests hold databases at once than there are databases: run fewer at once. Pools that were never disposed hold databases too, until their reservations end; the log names each pool that was finalized without being disposed.
              """;
   }
}
