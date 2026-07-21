using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Sql.Common;

public partial class DbConnectionPool<TConnection, TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   static readonly IThreadShared<Dictionary<string, IDbConnectionPool<TConnection, TCommand>>> Pools =
      IThreadShared.New(new Dictionary<string, IDbConnectionPool<TConnection, TCommand>>());

   public static IDbConnectionPool<TConnection, TCommand> ForConnectionString(string connectionString, Func<string, TConnection> createConnection) =>
      Pools.Locked(pools => pools.GetOrAdd(connectionString, constructor: () => new DbConnectionPool<TConnection, TCommand>(connectionString, createConnection)));
}
