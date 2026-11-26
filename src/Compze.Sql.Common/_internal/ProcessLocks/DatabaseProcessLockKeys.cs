using System.Security.Cryptography;
using System.Text;

namespace Compze.Sql.Common._internal.ProcessLocks;

///<summary>Derives the keys by which each database engine knows a lock of <see cref="IDatabaseProcessLocks"/> from the one identity that must<br/>
/// scope it: the database and the lock's name. Hashing makes the keys fit every engine's constraints, PostgreSQL's advisory locks keying<br/>
/// on a 64-bit integer, MySQL's lock names being server-wide and at most 64 characters long, a file name having no path separators, while<br/>
/// keeping distinct identities distinct.</summary>
static class DatabaseProcessLockKeys
{
   ///<summary>The 64-bit key PostgreSQL's advisory locks take.</summary>
   internal static long Int64Key(string databaseIdentity, string lockName) =>
      BitConverter.ToInt64(Hash(databaseIdentity, lockName), 0);

   ///<summary>A name that fits MySQL's 64-character limit on lock names, and is a file name: SQLite's lock is a file of that name.</summary>
   internal static string Name(string databaseIdentity, string lockName) =>
      $"CompzeDatabaseProcessLock_{Convert.ToHexString(Hash(databaseIdentity, lockName))[..38]}";

   static byte[] Hash(string databaseIdentity, string lockName) =>
      SHA256.HashData(Encoding.UTF8.GetBytes($"{databaseIdentity}|{lockName}"));
}
