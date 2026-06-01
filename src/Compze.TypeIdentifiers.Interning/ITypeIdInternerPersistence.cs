namespace Compze.TypeIdentifiers.Interning;

/// <summary>
/// Storage-specific persistence backing a <see cref="ITypeIdInterner"/>: the operations over the
/// <c>TypeIds</c> / <c>TypeStrings</c> / <c>TypeNames</c> tables.
/// </summary>
public interface ITypeIdInternerPersistence
{
   /// <summary>
   /// True when a minted type-id row commits independently of the business transaction and is durable the
   /// moment it is written (MVCC engines). False for single-writer engines (SQLite), where a mint joins the
   /// business transaction and can roll back — so the interner must not cache the <c>Type → id</c> direction.
   /// </summary>
   bool MintsAreImmediatelyDurable { get; }

   /// <summary>Ensures the type-identity tables exist.</summary>
   void EnsureInitialized();

   /// <summary>A lock-free full load of all type identities, spellings, and name-history heads.</summary>
   InternerSnapshot LoadAll();

   /// <summary>
   /// The conceptual id this spelling is recorded under, or null if it has never been persisted — a lock-free
   /// read used to answer "is this type interned?" without taking the write lock. On single-writer engines it
   /// observes the ambient transaction (so a type interned earlier in the same transaction is visible).
   /// </summary>
   int? FindIdBySpelling(string spelling);

   /// <summary>
   /// Runs <paramref name="work"/> holding the cross-process interner write lock on a single pinned
   /// connection (with the business transaction suppressed where the engine requires it). All reads and
   /// writes that <paramref name="work"/> performs go through the supplied <see cref="IInternerWriteSession"/>,
   /// which operates on that one connection.
   /// </summary>
   T MutateUnderWriteLock<T>(Func<IInternerWriteSession, T> work);
}
