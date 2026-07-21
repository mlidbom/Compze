using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite._internal;

namespace Compze.Internals.Sql.Sqlite.Wiring._internal;

static class SqliteSchemaContributionRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="_private.SqliteSchemaContribution"/> —<br/>
      /// to the schema of the database behind the endpoint's <see cref="ISqliteConnectionPool"/>, and on the first contribution registers the<br/>
      /// <see cref="SqliteSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
      /// Called by each sqlite feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
      internal IComponentRegistrar SqliteSchemaContribution(string schemaCreationSql)
      {
         @this.Register(Singleton.ForSet<_private.SqliteSchemaContribution>()
                                 .CreatedBy(() => new _private.SqliteSchemaContribution(schemaCreationSql)));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      ///<summary>The overload for schema-creation sql that only exists once the container can say what the tables are named —<br/>
      /// e.g. Tessaging's per-endpoint prefixed table-set: <paramref name="schemaCreationSql"/> is invoked at resolution time<br/>
      /// with the container's <typeparamref name="TTableNames"/>.</summary>
      internal IComponentRegistrar SqliteSchemaContribution<TTableNames>(Func<TTableNames, string> schemaCreationSql) where TTableNames : class
      {
         @this.Register(Singleton.ForSet<_private.SqliteSchemaContribution>()
                                 .CreatedBy((TTableNames tableNames) => new _private.SqliteSchemaContribution(schemaCreationSql(tableNames))));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      IComponentRegistrar SchemaManagerOnTheFirstContribution()
      {
         if(!@this.IsRegistered<SqliteSqlLayerSchemaManager>())
         {
            @this.Register(Singleton.For<SqliteSqlLayerSchemaManager>()
                                    .DelegateToParentServiceLocatorWhenCloning()
                                    .CreatedBy((ISqliteConnectionPool connectionPool, IComponentSet<_private.SqliteSchemaContribution> contributions)
                                                  => new SqliteSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
         }

         return @this;
      }
   }
}
