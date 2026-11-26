using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.Sqlite._internal;

namespace Compze.Sql.Sqlite.Wiring._internal;

static class SqliteDatabaseProcessLocksRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Registers the <see cref="IDatabaseProcessLocks"/> of the SQLite domain database behind the endpoint's<br/>
      /// <see cref="ISqliteConnectionPool"/>, unless they are registered already: called by each feature backend's registration that takes<br/>
      /// such a lock, so that several may.</summary>
      internal IComponentRegistrar SqliteDatabaseProcessLocks() =>
         @this.IsRegistered<IDatabaseProcessLocks>()
            ? @this
            : @this.Register(Singleton.For<IDatabaseProcessLocks>()
                                      .CreatedBy((ISqliteConnectionPool connectionPool) => new _private.SqliteDatabaseProcessLocks(connectionPool)));
   }
}
