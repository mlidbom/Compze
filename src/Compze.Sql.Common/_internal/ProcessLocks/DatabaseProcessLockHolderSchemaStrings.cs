namespace Compze.Sql.Common._internal.ProcessLocks;

///<summary>The table in which the holder of each of a database's <see cref="IDatabaseProcessLocks"/> records itself: a row for each lock<br/>
/// ever taken, naming the holder that last took it.</summary>
static class DatabaseProcessLockHolderSchemaStrings
{
   internal const string TableName = "DatabaseProcessLockHolder";
   internal const string LockName = nameof(LockName);
   internal const string Holder = nameof(Holder);

   ///<summary>The longest lock name the table holds.</summary>
   internal const int LockNameLength = 200;

   ///<summary>The longest holder description the table holds.</summary>
   internal const int HolderLength = 1000;
}
