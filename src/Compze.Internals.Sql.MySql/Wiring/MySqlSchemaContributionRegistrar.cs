using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MySql.Wiring;

static class MySqlSchemaContributionRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="Private.MySqlSchemaContribution"/> —<br/>
      /// to the schema of the database behind the endpoint's <see cref="IMySqlConnectionPool"/>, and on the first contribution registers the<br/>
      /// <see cref="Private.MySqlSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
      /// Called by each MySQL feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
      internal IComponentRegistrar MySqlSchemaContribution(string schemaCreationSql)
      {
         @this.Register(Singleton.ForSet<Private.MySqlSchemaContribution>()
                                 .CreatedBy(() => new Private.MySqlSchemaContribution(schemaCreationSql)));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      ///<summary>The overload for schema-creation sql that only exists once the container can say what the tables are named —<br/>
      /// e.g. Tessaging's per-endpoint prefixed table-set: <paramref name="schemaCreationSql"/> is invoked at resolution time<br/>
      /// with the container's <typeparamref name="TTableNames"/>.</summary>
      internal IComponentRegistrar MySqlSchemaContribution<TTableNames>(Func<TTableNames, string> schemaCreationSql) where TTableNames : class
      {
         @this.Register(Singleton.ForSet<Private.MySqlSchemaContribution>()
                                 .CreatedBy((TTableNames tableNames) => new Private.MySqlSchemaContribution(schemaCreationSql(tableNames))));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      IComponentRegistrar SchemaManagerOnTheFirstContribution()
      {
         if(!@this.IsRegistered<Private.MySqlSqlLayerSchemaManager>())
         {
            @this.Register(Singleton.For<Private.MySqlSqlLayerSchemaManager>()
                                    .DelegateToParentServiceLocatorWhenCloning()
                                    .CreatedBy((IMySqlConnectionPool connectionPool, IComponentSet<Private.MySqlSchemaContribution> contributions)
                                                  => new Private.MySqlSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
         }

         return @this;
      }
   }
}
