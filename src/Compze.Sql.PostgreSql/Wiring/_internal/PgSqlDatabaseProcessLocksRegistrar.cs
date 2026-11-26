using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.PostgreSql._internal;

namespace Compze.Sql.PostgreSql.Wiring._internal;

static class PgSqlDatabaseProcessLocksRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Registers the <see cref="IDatabaseProcessLocks"/> of the PostgreSQL domain database behind the endpoint's<br/>
      /// <see cref="IPgSqlConnectionPool"/>, and contributes their holder table to its schema, unless they are registered already: called by<br/>
      /// each feature backend's registration that takes such a lock, so that several may.</summary>
      internal IComponentRegistrar PgSqlDatabaseProcessLocks()
      {
         if(@this.IsRegistered<IDatabaseProcessLocks>()) return @this;
         return @this.PgSqlSchemaContribution(_private.PgSqlDatabaseProcessLocks.SchemaCreationStatements)
                     .Register(Singleton.For<IDatabaseProcessLocks>()
                                        .CreatedBy((IPgSqlConnectionPool connectionPool, PgSqlSqlLayerSchemaManager schemaManager) => new _private.PgSqlDatabaseProcessLocks(connectionPool, schemaManager)));
      }
   }
}
