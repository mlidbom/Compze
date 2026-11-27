using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Sql.Common._internal;

///<summary>The pools of their own that Compze keeps in this process on each database, beside the pool of the database's own connection<br/>
/// string, the one units of work hold their transactions' connections from.</summary>
///<remarks>A pool of its own serves operations that commit on their own, outside the ambient transaction, and that may run inside a unit<br/>
/// of work, such as the type-id interner's. Drawing from the units of work's pool, such an operation, in a unit of work that holds a lock,<br/>
/// waits for a connection while units of work waiting for that lock hold every one, and all of them wait until the pool times out. A pool<br/>
/// of its own holds no connection that a unit of work holds.</remarks>
///<remarks>The providers key their pools by connection string, so a pool of its own has a connection string of its own, derived from the<br/>
/// database's. Whatever clears a database's pool, as the DbPool does before it resets a database, clears those too: see<br/>
/// <see cref="ConnectionStringsOfEveryPoolOn"/>.</remarks>
static class PoolsOfTheirOwn
{
   static readonly IThreadShared<Dictionary<string, HashSet<string>>> ConnectionStringsByTheDatabasesConnectionString =
      IThreadShared.New(new Dictionary<string, HashSet<string>>());

   ///<summary>Records <paramref name="poolsConnectionString"/> as the connection string of a pool of its own on the database of<br/>
   /// <paramref name="databasesConnectionString"/>, and returns it.</summary>
   internal static string Record(string databasesConnectionString, string poolsConnectionString)
   {
      ConnectionStringsByTheDatabasesConnectionString.Locked(it => it.GetOrAdd(databasesConnectionString, constructor: () => new HashSet<string>(StringComparer.Ordinal)).Add(poolsConnectionString));
      return poolsConnectionString;
   }

   ///<summary>The connection strings of every pool Compze keeps in this process on the database of <paramref name="databasesConnectionString"/>:<br/>
   /// that one, and those of the pools of their own recorded on it.</summary>
   internal static IReadOnlyList<string> ConnectionStringsOfEveryPoolOn(string databasesConnectionString) =>
      ConnectionStringsByTheDatabasesConnectionString.Locked(it => (IReadOnlyList<string>)[databasesConnectionString, ..it.TryGetValue(databasesConnectionString, out var poolsOfTheirOwn) ? poolsOfTheirOwn : []]);
}
