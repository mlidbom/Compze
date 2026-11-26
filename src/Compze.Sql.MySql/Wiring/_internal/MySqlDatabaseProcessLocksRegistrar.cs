using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.MySql._internal;

namespace Compze.Sql.MySql.Wiring._internal;

static class MySqlDatabaseProcessLocksRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Registers the <see cref="IDatabaseProcessLocks"/> of the MySQL domain database behind the endpoint's<br/>
      /// <see cref="IMySqlConnectionPool"/>, and contributes their holder table to its schema, unless they are registered already: called by<br/>
      /// each feature backend's registration that takes such a lock, so that several may.</summary>
      internal IComponentRegistrar MySqlDatabaseProcessLocks()
      {
         if(@this.IsRegistered<IDatabaseProcessLocks>()) return @this;
         return @this.MySqlSchemaContribution(_private.MySqlDatabaseProcessLocks.SchemaCreationSql)
                     .Register(Singleton.For<IDatabaseProcessLocks>()
                                        .CreatedBy((IMySqlConnectionPool connectionPool, MySqlSqlLayerSchemaManager schemaManager) => new _private.MySqlDatabaseProcessLocks(connectionPool, schemaManager)));
      }
   }
}
