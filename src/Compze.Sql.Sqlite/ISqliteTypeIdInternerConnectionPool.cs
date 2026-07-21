namespace Compze.Sql.Sqlite;

/// <summary>
/// The connection pool for the type-id interner's <em>own</em> database — a second SQLite database, separate from
/// the one that holds a domain's business data.
/// </summary>
/// <remarks>
/// SQLite allows only one writer per database and does not tolerate a second connection writing the same database
/// while a transaction holds its write lock. Interning a type-id is reference-data bookkeeping that is deliberately
/// <em>not</em> part of a domain change's transaction (the other engines already commit it independently), so on
/// SQLite it is given its own database and the interner never opens a second connection to the business database.
/// <br/><br/>
/// This is its own type for one reason: the container resolves components by service type and cannot hold two
/// registrations of the same type, so the interner pool needs a type distinct from the business
/// <see cref="ISqliteConnectionPool"/> to coexist with it. It adds no members — it is the business pool's behaviour
/// pointed at a different database.
/// </remarks>
public interface ISqliteTypeIdInternerConnectionPool : ISqliteConnectionPool
{
   /// <summary>
   /// The business <see cref="ISqliteConnectionPool.SqliteConnectionPool"/> behaviour, tagged as the interner's
   /// pool so the container can hold both. Construct it for the interner database reached through
   /// <paramref name="getConnectionString"/>.
   /// </summary>
   public sealed class Pool(Func<string> getConnectionString) : SqliteConnectionPool(getConnectionString), ISqliteTypeIdInternerConnectionPool;
}
