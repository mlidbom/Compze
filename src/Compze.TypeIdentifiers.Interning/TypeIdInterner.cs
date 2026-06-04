using System.Collections.Concurrent;

namespace Compze.TypeIdentifiers.Interning;

/// <summary>
/// Engine-agnostic <see cref="ITypeIdInterner"/>: an in-memory model over the per-database type-identity
/// tables, loaded once on first use. A conceptual type owns one database-local <c>int</c> (<see cref="TypeId"/>
/// is the public currency; the canonical string is its spelling), and every spelling the type has ever been
/// persisted as resolves back to that one id — so a row written under any past spelling resolves through the id
/// to the type's current form. New spellings and renames are linked durably by a reconciliation pass at load
/// time; minting a never-before-seen type and that reconciliation both run under the persistence's cross-process
/// write lock.
/// </summary>
public sealed class TypeIdInterner : ITypeIdInterner
{
   readonly ITypeIdInternerPersistence _persistence;
   readonly ITypeMap _typeMap;
   readonly bool _cacheMints;

   // Lock-free reads; mutated under _writeLock during load, reconcile, and mint.
   readonly ConcurrentDictionary<int, Type> _idToType = new();
   readonly ConcurrentDictionary<Type, int> _typeToId = new();
   readonly ConcurrentDictionary<int, string> _currentNameById = new();
   readonly Lock _writeLock = new();
   volatile bool _loaded;

   /// <summary>Creates an interner backed by <paramref name="persistence"/>, using <paramref name="typeMap"/> to resolve persisted spellings to .NET types.</summary>
   public TypeIdInterner(ITypeIdInternerPersistence persistence, ITypeMap typeMap)
   {
      _persistence = persistence;
      _typeMap = typeMap;
      _cacheMints = persistence.MintsAreImmediatelyDurable;
   }

   /// <summary>Creates an <see cref="ITypeIdInterner"/> backed by <paramref name="persistence"/> and <paramref name="typeMap"/>.</summary>
   public static ITypeIdInterner For(ITypeIdInternerPersistence persistence, ITypeMap typeMap) => new TypeIdInterner(persistence, typeMap);

   /// <inheritdoc />
   public int GetOrInternId(TypeId typeId)
   {
      EnsureLoaded();
      if(_typeToId.TryGetValue(typeId.Type, out var existing))
         return existing;

      // Not in the cache. Another process may already have interned it, or a single-writer mint earlier in this
      // transaction may not be cached — so probe the database before minting.
      return TryResolveExistingId(typeId, out var found) ? found : Mint(typeId);
   }

   /// <inheritdoc />
   public bool TryGetInternedId(TypeId typeId, out int internedId)
   {
      EnsureLoaded();
      return _typeToId.TryGetValue(typeId.Type, out internedId) || TryResolveExistingId(typeId, out internedId);
   }

   bool TryResolveExistingId(TypeId typeId, out int internedId)
   {
      if(_persistence.FindIdBySpelling(typeId.CanonicalString) is {} id)
      {
         CacheResolved(typeId.Type, id);
         internedId = id;
         return true;
      }

      internedId = 0;
      return false;
   }

   // Records a id<->type resolution discovered from the database. The id -> Type direction is always safe to
   // cache (a row that does not survive commit is never referenced by surviving data). The Type -> id direction
   // is cached only on engines where a mint is immediately durable; on single-writer engines it is re-confirmed
   // against the database each time, so a rolled-back mint cannot leave a stale forward mapping.
   void CacheResolved(Type type, int id)
   {
      _idToType[id] = type;
      if(_cacheMints)
         _typeToId.AddOrUpdate(type, id, (_, current) => Math.Min(current, id));
   }

   /// <inheritdoc />
   public TypeId GetTypeId(int internedId)
   {
      EnsureLoaded();
      return _typeMap.GetId(ResolveType(internedId));
   }

   Type ResolveType(int internedId)
   {
      if(_idToType.TryGetValue(internedId, out var type))
         return type;

      // An id we have never seen: another process may have interned it since we loaded. Re-load and re-check;
      // if it still does not resolve, the spelling it was stored under no longer maps to a .NET type.
      lock(_writeLock)
      {
         if(_idToType.TryGetValue(internedId, out var caughtUp))
            return caughtUp;

         MergeSnapshot(_persistence.LoadAll());
         if(_idToType.TryGetValue(internedId, out var reloaded))
            return reloaded;

         throw UnresolvableId(internedId);
      }
   }

   int Mint(TypeId typeId)
   {
      lock(_writeLock)
      {
         if(_typeToId.TryGetValue(typeId.Type, out var alreadyMinted))
            return alreadyMinted;

         return _persistence.MutateUnderWriteLock(session =>
         {
            var spelling = typeId.CanonicalString;
            var name = NameOf(typeId.Type);

            // Idempotent: another process — or, on a single-writer engine, a prior mint whose business
            // transaction has since committed — may already hold this spelling (the lock-free probe in
            // GetOrInternId can race with another writer between releasing and re-acquiring).
            var id = session.FindBySpelling(spelling) ?? session.InsertType(name, spelling);

            _currentNameById.TryAdd(id, name);
            CacheResolved(typeId.Type, id);
            return id;
         });
      }
   }

   void EnsureLoaded()
   {
      if(_loaded)
         return;

      lock(_writeLock)
      {
         if(_loaded)
            return;

         _persistence.EnsureInitialized();
         var snapshot = _persistence.LoadAll();
         MergeSnapshot(snapshot);
         Reconcile(snapshot);
         _loaded = true;
      }
   }

   // Folds a snapshot into the in-memory model: records current names, and resolves each id's spellings to a
   // .NET type (one resolvable spelling is enough). Idempotent — ids already resolved are left untouched — so it
   // is safe to call repeatedly for catch-up.
   void MergeSnapshot(InternerSnapshot snapshot)
   {
      foreach(var (id, currentName) in snapshot.Types)
         _currentNameById[id] = currentName;

      foreach(var spellingsForId in snapshot.Spellings.GroupBy(spelling => spelling.TypeId))
      {
         if(_idToType.ContainsKey(spellingsForId.Key))
            continue;

         foreach(var (id, typeString) in spellingsForId)
            if(ResolveSpelling(typeString) is {} type)
            {
               MapTypeToId(type, id);
               break;
            }
      }
   }

   // Links each known type's current spelling and current name if the database is missing them — the durable
   // half of reclassification/rename handling. Only types this process can currently map are touched; foreign
   // types (another context's, present in a shared database) are skipped.
   void Reconcile(InternerSnapshot snapshot)
   {
      var nameById = snapshot.Types.ToDictionary(type => type.Id, type => type.CurrentName);
      var spellingsById = snapshot.Spellings
                                  .GroupBy(spelling => spelling.TypeId)
                                  .ToDictionary(group => group.Key, group => group.Select(spelling => spelling.TypeString).ToHashSet(StringComparer.Ordinal));

      var work = new List<(int Id, Type Type, string? SpellingToAdd, string? NameToRecord)>();
      foreach(var (type, id) in _typeToId.ToArray())
      {
         if(!_typeMap.TryGetId(type, out var current))
            continue;

         var currentSpelling = current.CanonicalString;
         var spellingMissing = !(spellingsById.TryGetValue(id, out var spellings) && spellings.Contains(currentSpelling));

         var currentName = NameOf(type);
         var nameChanged = !(nameById.TryGetValue(id, out var storedName) && string.Equals(storedName, currentName, StringComparison.Ordinal));

         if(spellingMissing || nameChanged)
            work.Add((id, type, spellingMissing ? currentSpelling : null, nameChanged ? currentName : null));
      }

      if(work.Count == 0)
         return;

      _persistence.MutateUnderWriteLock(session =>
      {
         foreach(var (id, type, spellingToAdd, nameToRecord) in work)
         {
            if(spellingToAdd != null && session.FindBySpelling(spellingToAdd) == null)
            {
               session.AddSpelling(id, spellingToAdd);
               MapTypeToId(type, id);
            }

            if(nameToRecord != null && !string.Equals(session.CurrentNameOf(id), nameToRecord, StringComparison.Ordinal))
            {
               session.RecordName(id, nameToRecord);
               _currentNameById[id] = nameToRecord;
            }
         }

         return 0;
      });
   }

   void MapTypeToId(Type type, int id)
   {
      _idToType[id] = type;
      _typeToId.AddOrUpdate(type, id, (_, current) => Math.Min(current, id));
   }

   Type? ResolveSpelling(string spelling)
   {
      try
      {
         return _typeMap.GetId(spelling).Type;
      }
      catch(Exception exception)when(exception is FormatException or InvalidOperationException or ArgumentException or TypeLoadException or FileNotFoundException or FileLoadException)
      {
         // The spelling no longer maps to a loadable type (a removed type, or a renamed one whose linking
         // mapping was never deployed). It contributes no Type -> id entry; an attempt to read data stored
         // under it surfaces as UnresolvableId.
         return null;
      }
   }

   static string NameOf(Type type) => TypeNameNormalization.StripAssemblyQualifiers(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);

   InvalidOperationException UnresolvableId(int internedId) =>
      _currentNameById.TryGetValue(internedId, out var lastKnownName)
         ? new InvalidOperationException(
            $"Interned type id {internedId}, last known as '{lastKnownName}', no longer resolves to a .NET type. If a type was renamed, the mapping change must be deployed and started once before the rename so its old and new names are linked; renaming in the same step strands the data written under the old name.")
         : new InvalidOperationException($"No interned type is recorded for id {internedId}.");
}
