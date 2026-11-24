using Compze.Sql.PostgreSql._internal;

namespace Compze.Sql.PostgreSql._private;

///<summary>One feature backend's part of the schema of the database behind the endpoint's <see cref="IPgSqlConnectionPool"/>:<br/>
/// the statements that create the backend's tables. Each backend's registration contributes its own — through<br/>
/// <see cref="Wiring._internal.PgSqlSchemaContributionRegistrar.PgSqlSchemaContribution"/>, as a component set member — and the<br/>
/// <see cref="PgSqlSqlLayerSchemaManager"/> resolves the whole set and creates every contributed schema before the database's<br/>
/// first use. So no composing layer ever needs to know which backends need what schemas.</summary>
///<remarks>Statements, each run as a command of its own, rather than a script of several: see <see cref="PgSqlSqlLayerSchemaManager"/>.</remarks>
sealed class PgSqlSchemaContribution
{
   internal PgSqlSchemaContribution(IReadOnlyList<string> schemaCreationStatements) => SchemaCreationStatements = schemaCreationStatements;

   ///<summary>The statements that create this feature backend's tables, in the order they run: each one statement, which may run more<br/>
   /// than once, as each endpoint and each process that joins the database runs it.</summary>
   internal IReadOnlyList<string> SchemaCreationStatements { get; }
}
