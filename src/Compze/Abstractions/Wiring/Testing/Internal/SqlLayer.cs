namespace Compze.Core.Wiring.Testing.Internal;

#pragma warning disable CA1724 //We don't much care that it has the same name as a namespace somewhere else.

public enum SqlLayer
{
   MsSql = 1,
   MySql = 2,
   PgSql = 3,
   Sqlite = 4,
   SqliteMemory = 5
}