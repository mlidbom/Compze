using System;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2.SystemExtensions;

interface IDB2ConnectionPool : IDbConnectionPool<IComposableDB2Connection, DB2Command>
{
   static IDB2ConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static IDB2ConnectionPool CreateInstance(Func<string> getConnectionString) => new DB2ConnectionPool(getConnectionString);

   class DB2ConnectionPool : IDB2ConnectionPool
   {
      readonly OptimizedLazy<IDbConnectionPool<IComposableDB2Connection, DB2Command>> _pool;

      public DB2ConnectionPool(Func<string> getConnectionString)
      {
         _pool = new OptimizedLazy<IDbConnectionPool<IComposableDB2Connection, DB2Command>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<IComposableDB2Connection, DB2Command>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                  IComposableDB2Connection.Create);
            });
      }

      public override string ToString() => $"MsSql::{_pool.ValueIfInitialized()?.ToString() ?? "Not initialized"}";
      public TResult UseConnection<TResult>(Func<IComposableDB2Connection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<IComposableDB2Connection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).CaF(); 
   }
}