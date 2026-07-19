using Compze.DependencyInjection;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

static class MsSqlSchemaContributionRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="Private.MsSqlSchemaContribution"/> —<br/>
      /// to the schema of the database behind the endpoint's <see cref="IMsSqlConnectionPool"/>, and on the first contribution registers the<br/>
      /// <see cref="Private.MsSqlSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
      /// Called by each MS SQL feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
      internal IComponentRegistrar MsSqlSchemaContribution(string schemaCreationSql)
      {
         @this.Register(Singleton.ForSet<Private.MsSqlSchemaContribution>()
                                 .CreatedBy(() => new Private.MsSqlSchemaContribution(schemaCreationSql)));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      ///<summary>The overload for schema-creation sql that only exists once the container can say what the tables are named —<br/>
      /// e.g. Tessaging's per-endpoint prefixed table-set: <paramref name="schemaCreationSql"/> is invoked at resolution time<br/>
      /// with the container's <typeparamref name="TTableNames"/>.</summary>
      internal IComponentRegistrar MsSqlSchemaContribution<TTableNames>(Func<TTableNames, string> schemaCreationSql) where TTableNames : class
      {
         @this.Register(Singleton.ForSet<Private.MsSqlSchemaContribution>()
                                 .CreatedBy((TTableNames tableNames) => new Private.MsSqlSchemaContribution(schemaCreationSql(tableNames))));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      IComponentRegistrar SchemaManagerOnTheFirstContribution()
      {
         if(!@this.IsRegistered<Private.MsSqlSqlLayerSchemaManager>())
         {
            @this.Register(Singleton.For<Private.MsSqlSqlLayerSchemaManager>()
                                    .DelegateToParentServiceLocatorWhenCloning()
                                    .CreatedBy((IMsSqlConnectionPool connectionPool, IComponentSet<Private.MsSqlSchemaContribution> contributions)
                                                  => new Private.MsSqlSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
         }

         return @this;
      }
   }
}
