using System.Security.Cryptography;
using System.Text;

namespace Compze.Tessaging._internal.SqlLayer;

///<summary>Derives the per-engine keys that name an endpoint's process lock<br/>
/// (see <see cref="ITessagingSqlLayer.IEndpointCatalogSqlLayer.TryTakeProcessLockAsync"/>) from the one identity that must<br/>
/// scope it: the database and the endpoint name. Hashing makes the keys safe for every engine's constraints — PostgreSQL's<br/>
/// advisory locks key on a 64-bit integer, MySQL lock names are server-wide and capped at 64 characters, OS mutex names<br/>
/// forbid path separators — while keeping distinct identities distinct.</summary>
static class ProcessLockKeys
{
   ///<summary>The 64-bit key PostgreSQL's advisory locks take.</summary>
   internal static long Int64Key(string databaseIdentity, string endpointName) =>
      BitConverter.ToInt64(Hash(databaseIdentity, endpointName), 0);

   ///<summary>A lock name that fits MySQL's 64-character cap on lock names and the OS's mutex-name rules alike.</summary>
   internal static string Name(string databaseIdentity, string endpointName) =>
      $"CompzeEndpointProcessLock_{Convert.ToHexString(Hash(databaseIdentity, endpointName))[..38]}";

   static byte[] Hash(string databaseIdentity, string endpointName) =>
      SHA256.HashData(Encoding.UTF8.GetBytes($"{databaseIdentity}|{endpointName}"));
}
