using System;
using System.Collections.Generic;
using System.Data.Common;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Sql.Common;

public abstract partial class DbConnectionPool<TConnection, TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   static readonly IThreadShared<Dictionary<string, IDbConnectionPool<TConnection, TCommand>>> Pools =
      IThreadShared.New(new Dictionary<string, IDbConnectionPool<TConnection, TCommand>>());

   public static IDbConnectionPool<TConnection, TCommand> ForConnectionString(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection) =>
      Pools.Locked(pools => pools.GetOrAdd(connectionString, constructor: () => Create(connectionString, flags, createConnection)));

   static IDbConnectionPool<TConnection, TCommand> Create(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection) =>
      flags.HasFlag(PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction)
         ? new TransactionAffinityDbConnectionPool(connectionString, createConnection)
         : new DefaultDbConnectionPool(connectionString, createConnection);
}