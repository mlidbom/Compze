using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.PostgreSql.Wiring.Internal;

static class PgSqlSchemaContributionRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="Private.PgSqlSchemaContribution"/> —<br/>
      /// to the schema of the database behind the endpoint's <see cref="IPgSqlConnectionPool"/>, and on the first contribution registers the<br/>
      /// <see cref="Private.PgSqlSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
      /// Called by each PostgreSQL feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
      internal IComponentRegistrar PgSqlSchemaContribution(string schemaCreationSql)
      {
         @this.Register(Singleton.ForSet<Private.PgSqlSchemaContribution>()
                                 .CreatedBy(() => new Private.PgSqlSchemaContribution(schemaCreationSql)));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      ///<summary>The overload for schema-creation sql that only exists once the container can say what the tables are named —<br/>
      /// e.g. Tessaging's per-endpoint prefixed table-set: <paramref name="schemaCreationSql"/> is invoked at resolution time<br/>
      /// with the container's <typeparamref name="TTableNames"/>.</summary>
      internal IComponentRegistrar PgSqlSchemaContribution<TTableNames>(Func<TTableNames, string> schemaCreationSql) where TTableNames : class
      {
         @this.Register(Singleton.ForSet<Private.PgSqlSchemaContribution>()
                                 .CreatedBy((TTableNames tableNames) => new Private.PgSqlSchemaContribution(schemaCreationSql(tableNames))));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      IComponentRegistrar SchemaManagerOnTheFirstContribution()
      {
         if(!@this.IsRegistered<Private.PgSqlSqlLayerSchemaManager>())
         {
            @this.Register(Singleton.For<Private.PgSqlSqlLayerSchemaManager>()
                                    .DelegateToParentServiceLocatorWhenCloning()
                                    .CreatedBy((IPgSqlConnectionPool connectionPool, IComponentSet<Private.PgSqlSchemaContribution> contributions)
                                                  => new Private.PgSqlSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
         }

         return @this;
      }
   }
}
