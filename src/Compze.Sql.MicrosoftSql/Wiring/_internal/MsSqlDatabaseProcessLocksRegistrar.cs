using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.MicrosoftSql._internal;

namespace Compze.Sql.MicrosoftSql.Wiring._internal;

static class MsSqlDatabaseProcessLocksRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Registers the <see cref="IDatabaseProcessLocks"/> of the SQL Server domain database behind the endpoint's<br/>
      /// <see cref="IMsSqlConnectionPool"/>, and contributes their holder table to its schema, unless they are registered already: called by<br/>
      /// each feature backend's registration that takes such a lock, so that several may.</summary>
      internal IComponentRegistrar MsSqlDatabaseProcessLocks()
      {
         if(@this.IsRegistered<IDatabaseProcessLocks>()) return @this;
         return @this.MsSqlSchemaContribution(_private.MsSqlDatabaseProcessLocks.SchemaCreationSql)
                     .Register(Singleton.For<IDatabaseProcessLocks>()
                                        .CreatedBy((IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager) => new _private.MsSqlDatabaseProcessLocks(connectionPool, schemaManager)));
      }
   }
}
