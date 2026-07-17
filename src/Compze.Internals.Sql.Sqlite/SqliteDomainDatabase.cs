namespace Compze.Internals.Sql.Sqlite;

///<summary>The declaration that the domain database an endpoint joins is Sqlite. What it carries beyond the engine choice is<br/>
/// the <see cref="ConnectionStringName"/> the Sqlite pairings derive their wiring from — e.g. the type-id interner's own<br/>
/// database name (<c>SqliteTypeIdInterner(...)</c>), which on sqlite lives in a separate database file.</summary>
public class SqliteDomainDatabase
{
   ///<summary>The name whose configured connection string reaches the domain database.</summary>
   public string ConnectionStringName { get; }

   public SqliteDomainDatabase(string connectionStringName) => ConnectionStringName = connectionStringName;
}
