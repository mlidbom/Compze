namespace Compze.Internals.Sql.PostgreSql;

///<summary>The declaration that an endpoint's database is PostgreSql: carried by <c>EndpointFoundation&lt;PgSqlEndpointDatabase&gt;</c>,<br/>
/// through which the features added on the foundation bind their sql layers to the PgSql engine — a feature's PgSql pairing<br/>
/// applies only to an endpoint declaring this database.</summary>
public class PgSqlEndpointDatabase
{
   ///<summary>The name whose configured connection string reaches the endpoint's database.</summary>
   public string ConnectionStringName { get; }

   public PgSqlEndpointDatabase(string connectionStringName) => ConnectionStringName = connectionStringName;
}
