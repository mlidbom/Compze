using Compze.Internals.Sql.MicrosoftSql._internal;

namespace Compze.Internals.Sql.MicrosoftSql._private;

///<summary>One feature backend's part of the schema of the database behind the endpoint's <see cref="IMsSqlConnectionPool"/>:<br/>
/// the SQL that creates the backend's tables. Each backend's registration contributes its own — through<br/>
/// <see cref="Wiring._internal.MsSqlSchemaContributionRegistrar.MsSqlSchemaContribution"/>, as a component set member — and the<br/>
/// <see cref="MsSqlSqlLayerSchemaManager"/> resolves the whole set and creates every contributed schema in a single batch<br/>
/// before the database's first use. So no composing layer ever needs to know which backends need what schemas.</summary>
sealed class MsSqlSchemaContribution(string schemaCreationSql)
{
   ///<summary>The SQL that creates this feature backend's tables.</summary>
   internal string SchemaCreationSql { get; } = schemaCreationSql;
}
