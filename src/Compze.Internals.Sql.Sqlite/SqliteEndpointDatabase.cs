namespace Compze.Internals.Sql.Sqlite;

///<summary>The declaration that an endpoint's database is Sqlite. What it carries beyond the engine choice is the<br/>
/// <see cref="ConnectionStringName"/> the Sqlite pairings derive their wiring from — e.g. the type-id interner's own database<br/>
/// name (<c>SqliteTypeIdInterner(...)</c>), which on sqlite lives in a separate database file.</summary>
public class SqliteEndpointDatabase
{
   ///<summary>The name whose configured connection string reaches the endpoint's database.</summary>
   public string ConnectionStringName { get; }

   public SqliteEndpointDatabase(string connectionStringName) => ConnectionStringName = connectionStringName;
}
