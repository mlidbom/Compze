using System.Collections.Concurrent;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Npgsql;

namespace Compze.Sql.PostgreSql._private;

///<summary>The gate Compze's connections to one PostgreSQL database pass before opening, which admits as many at once as Npgsql's pool<br/>
/// for the connection string holds connections, its <c>Max Pool Size</c>. A connection takes a place as it opens, and gives it back once<br/>
/// it has returned its connection to Npgsql's pool. So Npgsql's pool always has a connection for an open, and the open never waits there.</summary>
///<remarks>Why: a synchronous open that waits for Npgsql's pool needs a free thread-pool thread to be woken. Npgsql's waiter sleeps on an<br/>
/// event that a continuation of its idle-connection channel sets, and the channel runs that continuation on the thread pool. With<br/>
/// every pool thread busy, as under many units of work at once, each waiting for a connection, the returned connections lay unclaimed<br/>
/// until the pool added a thread: a hundred and twenty-eight units of work through a pool of ten took 12 s on PostgreSQL, and under 1 s<br/>
/// on SQL Server and MySQL, whose providers wake a synchronous waiter directly. A <see cref="SemaphoreSlim"/> does too: its release<br/>
/// wakes a synchronous waiter itself, on the releasing thread. Npgsql 11 still waits this way.</remarks>
///<remarks>One semaphore per connection string, held for the process's lifetime by <see cref="GatesByConnectionString"/>: the same<br/>
/// lifetime and key as Npgsql's pools, and as the connection pools. A connection string with pooling off has no pool to wait for, and<br/>
/// its gate admits every connection.</remarks>
sealed class NpgsqlPoolGate
{
   static readonly ConcurrentDictionary<string, NpgsqlPoolGate> GatesByConnectionString = new();

   readonly SemaphoreSlim? _placesInThePool;
   readonly TimeSpan _timeout;
   readonly string _exhaustedMessage;

   NpgsqlPoolGate(NpgsqlConnectionStringBuilder connectionString)
   {
      _placesInThePool = connectionString.Pooling ? new SemaphoreSlim(connectionString.MaxPoolSize, connectionString.MaxPoolSize) : null;
      _timeout = TimeSpan.FromSeconds(connectionString.Timeout);
      _exhaustedMessage = $"All {connectionString.MaxPoolSize} connections of the pool for the PostgreSQL database {connectionString.Database} on {connectionString.Host} were in use for {connectionString.Timeout} seconds, its Timeout: raise its 'Max Pool Size' or its 'Timeout' in the connection string, or use fewer connections at once.";
   }

   ///<summary>The gate of the pool that Npgsql keeps for <paramref name="connectionString"/>.</summary>
   internal static NpgsqlPoolGate OfThePoolFor(string connectionString) =>
      GatesByConnectionString.GetOrAdd(connectionString, _ => new NpgsqlPoolGate(new NpgsqlConnectionStringBuilder(connectionString)));

   ///<summary>Waits for a place in the pool, at most the connection string's <c>Timeout</c>, and takes it.</summary>
   internal void Take()
   {
      if(_placesInThePool?.Wait(_timeout) == false) throw new TimeoutException(_exhaustedMessage);
   }

   ///<summary>Awaits a place in the pool, at most the connection string's <c>Timeout</c>, and takes it.</summary>
   internal async Task TakeAsync()
   {
      if(_placesInThePool != null && !await _placesInThePool.WaitAsync(_timeout).caf()) throw new TimeoutException(_exhaustedMessage);
   }

   ///<summary>Gives back a place taken, once its connection is back in Npgsql's pool.</summary>
   internal void Release() => _placesInThePool?.Release();
}
