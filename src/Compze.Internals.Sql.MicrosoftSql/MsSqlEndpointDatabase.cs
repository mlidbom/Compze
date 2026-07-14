namespace Compze.Internals.Sql.MicrosoftSql;

///<summary>The declaration that an endpoint's database is MicrosoftSql: carried by <c>EndpointFoundation&lt;MsSqlEndpointDatabase&gt;</c>,<br/>
/// through which the features added on the foundation bind their sql layers to the MsSql engine — a feature's MsSql pairing<br/>
/// applies only to an endpoint declaring this database.</summary>
public class MsSqlEndpointDatabase
{
   ///<summary>The name whose configured connection string reaches the endpoint's database.</summary>
   public string ConnectionStringName { get; }

   public MsSqlEndpointDatabase(string connectionStringName) => ConnectionStringName = connectionStringName;
}
