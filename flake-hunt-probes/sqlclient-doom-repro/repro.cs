#:package Microsoft.Data.SqlClient@6.1.3
#:property TargetFramework=net11.0
using System.Transactions;
using Microsoft.Data.SqlClient;

var cs = Environment.GetEnvironmentVariable("COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING")! + "Application Name=doom-repro;";
SqlConnection.ClearAllPools();
var secondOpened = new ManualResetEventSlim();
var sqlClientsHandlerHasRun = new ManualResetEventSlim();
Guid firstPhysical = default, secondPhysical = default;
Task second = Task.CompletedTask;

using(var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
   var first = new SqlConnection(cs);
   //Like Compze's pool: the handler that closes the connection is subscribed before the connection opens and enlists.
   Transaction.Current!.TransactionCompleted += (_, _) =>
   {
      first.Close(); //The physical connection goes back to the pool, before SqlClient's own handler for this transaction has run.
      second = Task.Run(() =>
      {
         using var secondScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
         using var connection = new SqlConnection(cs);
         connection.Open(); //Takes the same physical connection from the pool, and enlists it in the second transaction.
         secondPhysical = connection.ClientConnectionId;
         secondOpened.Set();
         sqlClientsHandlerHasRun.Wait();
         using var command = connection.CreateCommand();
         command.CommandText = "SELECT 1";
         Console.WriteLine($"second transaction's command: {command.ExecuteScalar()}");
         secondScope.Complete();
      });
      secondOpened.Wait();
   };
   first.Open();
   firstPhysical = first.ClientConnectionId;
   using(var command = first.CreateCommand()) { command.CommandText = "SELECT 1"; command.ExecuteScalar(); }
   scope.Complete();
}
sqlClientsHandlerHasRun.Set(); //The completing scope's Dispose returned: every completion handler, SqlClient's included, has run.
try
{
   await second;
   Console.WriteLine("second transaction committed");
}
catch(Exception e)
{
   Console.WriteLine($"second transaction failed: {e.GetType().Name}: {e.Message} / {e.InnerException?.GetType().Name}: {e.InnerException?.Message}");
}
Console.WriteLine($"same physical connection: {firstPhysical == secondPhysical}");
