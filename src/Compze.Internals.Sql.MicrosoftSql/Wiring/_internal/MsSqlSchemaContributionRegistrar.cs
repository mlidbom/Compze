using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql._internal;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring._internal;

static class MsSqlSchemaContributionRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="_private.MsSqlSchemaContribution"/> —<br/>
      /// to the schema of the database behind the endpoint's <see cref="IMsSqlConnectionPool"/>, and on the first contribution registers the<br/>
      /// <see cref="MsSqlSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
      /// Called by each MS SQL feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
      internal IComponentRegistrar MsSqlSchemaContribution(string schemaCreationSql)
      {
         @this.Register(Singleton.ForSet<_private.MsSqlSchemaContribution>()
                                 .CreatedBy(() => new _private.MsSqlSchemaContribution(schemaCreationSql)));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      ///<summary>The overload for schema-creation sql that only exists once the container can say what the tables are named —<br/>
      /// e.g. Tessaging's per-endpoint prefixed table-set: <paramref name="schemaCreationSql"/> is invoked at resolution time<br/>
      /// with the container's <typeparamref name="TTableNames"/>.</summary>
      internal IComponentRegistrar MsSqlSchemaContribution<TTableNames>(Func<TTableNames, string> schemaCreationSql) where TTableNames : class
      {
         @this.Register(Singleton.ForSet<_private.MsSqlSchemaContribution>()
                                 .CreatedBy((TTableNames tableNames) => new _private.MsSqlSchemaContribution(schemaCreationSql(tableNames))));

         return @this.SchemaManagerOnTheFirstContribution();
      }

      IComponentRegistrar SchemaManagerOnTheFirstContribution()
      {
         if(!@this.IsRegistered<MsSqlSqlLayerSchemaManager>())
         {
            @this.Register(Singleton.For<MsSqlSqlLayerSchemaManager>()
                                    .DelegateToParentServiceLocatorWhenCloning()
                                    .CreatedBy((IMsSqlConnectionPool connectionPool, IComponentSet<_private.MsSqlSchemaContribution> contributions)
                                                  => new MsSqlSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
         }

         return @this;
      }
   }
}
