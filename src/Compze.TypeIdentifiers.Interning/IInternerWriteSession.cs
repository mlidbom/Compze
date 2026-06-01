namespace Compze.TypeIdentifiers.Interning;

/// <summary>
/// The write side of the interner, scoped to a single pinned connection that already holds the cross-process
/// interner write lock. Every method runs on that one connection (opening a second pooled connection while one
/// is held deadlocks the pool), so the interner's reconciliation and mint logic drives all of its lock-held
/// reads and writes through here. <c>FirstSeenUtc</c> stamps and name-history sequence numbers are produced by
/// the engine implementation (from its injected time source, and a <c>MAX(Seq)+1</c> over the type's history).
/// </summary>
public interface IInternerWriteSession
{
   /// <summary>The conceptual id this spelling is recorded under, or null if it has never been persisted.</summary>
   int? FindBySpelling(string spelling);

   /// <summary>The current name recorded for <paramref name="typeId"/>, or null if there is no such type.</summary>
   string? CurrentNameOf(int typeId);

   /// <summary>
   /// Mints a brand-new conceptual type: inserts its identity row (with <paramref name="fullyQualifiedName"/>
   /// as the current name), its first spelling, and its first name-history entry. Returns the new id.
   /// </summary>
   int InsertType(string fullyQualifiedName, string spelling);

   /// <summary>Records an additional spelling for an existing conceptual type (a reclassification link).</summary>
   void AddSpelling(int typeId, string spelling);

   /// <summary>
   /// Appends the next name-history entry for an existing type and updates its current name (a rename observed).
   /// </summary>
   void RecordName(int typeId, string fullyQualifiedName);
}
