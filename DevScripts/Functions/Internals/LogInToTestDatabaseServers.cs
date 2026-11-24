#:package Microsoft.Data.SqlClient@6.1.3
#:package Npgsql@9.0.4
#:package MySqlConnector@2.6.1
#:property NuGetAuditMode=direct

// Logs in to the test database servers C-Set-TestDatabaseServers is about to choose, with the connection strings it is
// about to set, so that a wrong password or a server that refuses this machine fails the command instead of the tests.
// Arguments: the SQL Server, PostgreSQL and MySQL connection strings, in that order. Writes a line for each server it
// can't log in to, and exits with 1 if there is one. Run with: dotnet run --file LogInToTestDatabaseServers.cs -- <three>

using System.Data.Common;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

if(args.Length != 3)
{
   Console.Error.WriteLine("Give the SQL Server, PostgreSQL and MySQL connection strings, in that order.");
   return 2;
}

var failures = new[]
               {
                  FailureToLogIn("SQL Server", new SqlConnection(args[0])),
                  FailureToLogIn("PostgreSQL", new NpgsqlConnection(args[1])),
                  FailureToLogIn("MySQL", new MySqlConnection(args[2]))
               }.OfType<string>().ToList();

failures.ForEach(Console.WriteLine);
return failures.Count == 0 ? 0 : 1;

static string? FailureToLogIn(string server, DbConnection connection)
{
   using(connection)
   {
      try
      {
         connection.Open();
         return null;
      }
      catch(DbException exception) //What this program reports: why the server refused, such as a wrong password.
      {
         return $"{server}: {exception.Message}";
      }
   }
}
