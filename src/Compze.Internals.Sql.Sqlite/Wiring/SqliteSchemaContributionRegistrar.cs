using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

static class SqliteSchemaContributionRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="Private.SqliteSchemaContribution"/> —<br/>
      /// to the schema of the database behind the endpoint's <see cref="ISqliteConnectionPool"/>, and on the first contribution registers the<br/>
      /// <see cref="Private.SqliteSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
      /// Called by each sqlite feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
      internal IComponentRegistrar SqliteSchemaContribution(string schemaCreationSql)
      {
         @this.Register(Singleton.ForSet<Private.SqliteSchemaContribution>()
                                 .CreatedBy(() => new Private.SqliteSchemaContribution(schemaCreationSql)));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      ///<summary>The overload for schema-creation sql that only exists once the container can say what the tables are named —<br/>
      /// e.g. Tessaging's per-endpoint prefixed table-set: <paramref name="schemaCreationSql"/> is invoked at resolution time<br/>
      /// with the container's <typeparamref name="TTableNames"/>.</summary>
      internal IComponentRegistrar SqliteSchemaContribution<TTableNames>(Func<TTableNames, string> schemaCreationSql) where TTableNames : class
      {
         @this.Register(Singleton.ForSet<Private.SqliteSchemaContribution>()
                                 .CreatedBy((TTableNames tableNames) => new Private.SqliteSchemaContribution(schemaCreationSql(tableNames))));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      IComponentRegistrar SchemaManagerOnTheFirstContribution()
      {
         if(!@this.IsRegistered<Private.SqliteSqlLayerSchemaManager>())
         {
            @this.Register(Singleton.For<Private.SqliteSqlLayerSchemaManager>()
                                    .DelegateToParentServiceLocatorWhenCloning()
                                    .CreatedBy((ISqliteConnectionPool connectionPool, IComponentSet<Private.SqliteSchemaContribution> contributions)
                                                  => new Private.SqliteSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
         }

         return @this;
      }
   }
}
