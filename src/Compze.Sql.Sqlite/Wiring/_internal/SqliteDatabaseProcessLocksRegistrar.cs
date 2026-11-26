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
      /// <see cref="ISqliteConnectionPool"/>, and contributes their holder table to its schema, unless they are registered already: called by<br/>
      /// each feature backend's registration that takes such a lock, so that several may.</summary>
      internal IComponentRegistrar SqliteDatabaseProcessLocks()
      {
         if(@this.IsRegistered<IDatabaseProcessLocks>()) return @this;
         return @this.SqliteSchemaContribution(_private.SqliteDatabaseProcessLocks.SchemaCreationSql)
                     .Register(Singleton.For<IDatabaseProcessLocks>()
                                        .CreatedBy((ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager) => new _private.SqliteDatabaseProcessLocks(connectionPool, schemaManager)));
      }
   }
}
