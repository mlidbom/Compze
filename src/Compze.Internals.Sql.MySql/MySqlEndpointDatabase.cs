namespace Compze.Internals.Sql.MySql;

///<summary>The declaration that an endpoint's database is MySql: carried by <c>EndpointFoundation&lt;MySqlEndpointDatabase&gt;</c>,<br/>
/// through which the features added on the foundation bind their sql layers to the MySql engine — a feature's MySql pairing<br/>
/// applies only to an endpoint declaring this database.</summary>
public class MySqlEndpointDatabase
{
   ///<summary>The name whose configured connection string reaches the endpoint's database.</summary>
   public string ConnectionStringName { get; }

   public MySqlEndpointDatabase(string connectionStringName) => ConnectionStringName = connectionStringName;
}
