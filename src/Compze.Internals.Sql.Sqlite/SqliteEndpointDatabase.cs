namespace Compze.Internals.Sql.Sqlite;

///<summary>The declaration that an endpoint's database is Sqlite: carried by <c>EndpointFoundation&lt;SqliteEndpointDatabase&gt;</c>,<br/>
/// through which the features added on the foundation bind their sql layers to the Sqlite engine — a feature's Sqlite pairing<br/>
/// applies only to an endpoint declaring this database.</summary>
public class SqliteEndpointDatabase
{
   ///<summary>The name whose configured connection string reaches the endpoint's database.</summary>
   public string ConnectionStringName { get; }

   public SqliteEndpointDatabase(string connectionStringName) => ConnectionStringName = connectionStringName;
}
