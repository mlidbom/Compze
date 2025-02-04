using System;
using System.Collections.Generic;
using System.Data.Common;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Persistence.Common.AdoCE;

abstract partial class DbConnectionManager<TConnection, TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   static readonly IThreadShared<Dictionary<string, IDbConnectionPool<TConnection, TCommand>>> Pools =
      ThreadShared.WithDefaultTimeout(new Dictionary<string, IDbConnectionPool<TConnection, TCommand>>());

   internal static IDbConnectionPool<TConnection, TCommand> ForConnectionString(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection) =>
      Pools.Update(pools => pools.GetOrAdd(connectionString, constructor: () => Create(connectionString, flags, createConnection)));

   static IDbConnectionPool<TConnection, TCommand> Create(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection) =>
      flags.HasFlag(PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction)
         ? new TransactionAffinityDbConnectionManager(connectionString, createConnection)
         : new DefaultDbConnectionManager(connectionString, createConnection);
}