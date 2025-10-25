namespace Compze.Core.Wiring.Testing.Internal;

#pragma warning disable CA1724 //We don't much care that it has the same name as a namespace somewhere else.

public enum SqlLayer
{
   MicrosoftSqlServer,
   MySql,
   PostgreSql,
   Sqlite,
   SqliteMemory
}