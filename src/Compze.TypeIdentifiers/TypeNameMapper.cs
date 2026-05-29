using System.Collections.Concurrent;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Transforms between .NET <see cref="Type"/> objects and <see cref="TypeIdentifier"/> values.
/// Supports incremental registration of assemblies. Caches results in both directions.
/// <para>Thread-safe by construction: all mapping data lives in an immutable <see cref="State"/> snapshot held
/// in a single <c>volatile</c> field. Registration builds a new snapshot (copy-on-write) and atomically swaps it.
/// Each snapshot owns its own caches, so a lookup that is computing against an older snapshot when a registration
/// swaps in a new one stores its result in the now-abandoned snapshot's cache — it can never poison the current
/// snapshot. Reads are lock-free; only registration takes a lock, and only to serialize writers.</para>
/// </summary>
class TypeNameMapper
{
   volatile State _state = State.Empty;
   readonly object _writeLock = new();

   internal void AddLeafTypeMapping(Type type, Guid guid)
   {
      lock(_writeLock)
         _state = _state.WithLeafMapping(type, guid);
   }

   internal void AddOpenGenericMapping(Type openGenericType, Guid guid)
   {
      lock(_writeLock)
         _state = _state.WithOpenGenericMapping(openGenericType, guid);
   }

   internal void AddStableAssemblyName(string assemblyName)
   {
      lock(_writeLock)
         _state = _state.WithStableAssembly(assemblyName);
   }

   internal bool TryGetOpenGenericMapping(Type openGenericType, out MappedTypeIdentifier id) => _state.TryGetOpenGenericMapping(openGenericType, out id);
   internal bool HasMappingForOpenGeneric(Type openGenericType) => _state.HasMappingForOpenGeneric(openGenericType);

   internal bool HasLeafMapping(Type type) => _state.HasLeafMapping(type);
   internal bool IsStableType(Type type) => _state.IsStableType(type);

   /// <summary>Given a .NET <see cref="Type"/>, produce the correct <see cref="TypeIdentifier"/> subtype.</summary>
   internal TypeIdentifier GetId(Type type) => _state.GetId(type);

   /// <summary>Given a <see cref="TypeIdentifier"/>, resolve it back to a .NET <see cref="Type"/>.</summary>
   internal Type GetType(TypeIdentifier typeId) => _state.GetType(typeId);

   /// <summary>
   /// Given a string from a persisted <c>$type</c> field, resolve it to a .NET <see cref="Type"/>.
   /// This is the deserialization entry point — the string is parsed, then resolved.
   /// </summary>
   internal Type GetTypeFromPersistedString(string persistedTypeString) => _state.GetTypeFromPersistedString(persistedTypeString);

   /// <summary>
   /// Given a raw <c>AssemblyQualifiedName</c> from Newtonsoft (serialize direction),
   /// produce the persisted string with mapped components replaced by GUIDs.
   /// </summary>
   internal string GetPersistedStringFromAssemblyQualifiedName(string assemblyQualifiedName) => _state.GetPersistedStringFromAssemblyQualifiedName(assemblyQualifiedName);

   /// <summary>
   /// An immutable snapshot of all type mappings plus the caches derived from them. Registration produces a new
   /// snapshot rather than mutating; this is what makes the mapper thread-safe and free of cache-invalidation races.
   /// </summary>
   sealed class State : ITypeMappingLookup
   {
      internal static readonly State Empty = new(new(), new(), new(), new(), new());

      // Mapping data — fixed for the life of this snapshot.
      readonly Dictionary<Guid, Type> _guidToLeafType;
      readonly Dictionary<Type, Guid> _leafTypeToGuid;
      readonly Dictionary<Guid, Type> _guidToOpenGeneric;
      readonly Dictionary<Type, Guid> _openGenericToGuid;
      readonly HashSet<string> _stableAssemblyNames;

      // Caches — lazily populated, and only ever hold values consistent with this snapshot's mappings.
      readonly ConcurrentDictionary<Type, TypeIdentifier> _typeToIdentifier = new();
      readonly ConcurrentDictionary<string, Type> _stringToType = new();

      State(Dictionary<Guid, Type> guidToLeafType,
            Dictionary<Type, Guid> leafTypeToGuid,
            Dictionary<Guid, Type> guidToOpenGeneric,
            Dictionary<Type, Guid> openGenericToGuid,
            HashSet<string> stableAssemblyNames)
      {
         _guidToLeafType = guidToLeafType;
         _leafTypeToGuid = leafTypeToGuid;
         _guidToOpenGeneric = guidToOpenGeneric;
         _openGenericToGuid = openGenericToGuid;
         _stableAssemblyNames = stableAssemblyNames;
      }

      internal State WithLeafMapping(Type type, Guid guid)
      {
         AssertTypeAndGuidAreUnmapped(type, guid, _leafTypeToGuid, _guidToLeafType);
         return new State(
            new Dictionary<Guid, Type>(_guidToLeafType) { [guid] = type },
            new Dictionary<Type, Guid>(_leafTypeToGuid) { [type] = guid },
            _guidToOpenGeneric,
            _openGenericToGuid,
            _stableAssemblyNames);
      }

      internal State WithOpenGenericMapping(Type openGenericType, Guid guid)
      {
         AssertTypeAndGuidAreUnmapped(openGenericType, guid, _openGenericToGuid, _guidToOpenGeneric);
         return new State(
            _guidToLeafType,
            _leafTypeToGuid,
            new Dictionary<Guid, Type>(_guidToOpenGeneric) { [guid] = openGenericType },
            new Dictionary<Type, Guid>(_openGenericToGuid) { [openGenericType] = guid },
            _stableAssemblyNames);
      }

      internal State WithStableAssembly(string assemblyName) =>
         new(_guidToLeafType, _leafTypeToGuid, _guidToOpenGeneric, _openGenericToGuid,
             new HashSet<string>(_stableAssemblyNames) { assemblyName });

      // A type↔GUID mapping is permanent identity. Re-mapping a type, or binding a GUID to a second type, silently
      // corrupts the reverse lookup and makes already-persisted data resolve to the wrong type. This is the funnel
      // every registration passes through — including across assemblies — so it is where both are rejected.
      static void AssertTypeAndGuidAreUnmapped(Type type, Guid guid, Dictionary<Type, Guid> typeToGuid, Dictionary<Guid, Type> guidToType)
      {
         if(typeToGuid.TryGetValue(type, out var existingGuid))
            throw new InvalidOperationException(
               $"Type '{type.FullName}' is already mapped to GUID '{existingGuid}'. A type may be mapped only once.");

         if(guidToType.TryGetValue(guid, out var existingType))
            throw new InvalidOperationException(
               $"GUID '{guid}' is already mapped to type '{existingType.FullName}' and cannot also be mapped to '{type.FullName}'. Each type must have its own GUID.");
      }

      internal bool TryGetOpenGenericMapping(Type openGenericType, out MappedTypeIdentifier id)
      {
         if(_openGenericToGuid.TryGetValue(openGenericType, out var guid))
         {
            id = new MappedTypeIdentifier(guid);
            return true;
         }
         id = default!;
         return false;
      }

      internal bool HasMappingForOpenGeneric(Type openGenericType) => _openGenericToGuid.ContainsKey(openGenericType);
      internal bool HasLeafMapping(Type type) => _leafTypeToGuid.ContainsKey(type);

      internal bool IsStableType(Type type)
      {
         var assemblyName = type.Assembly.GetName().Name;
         return assemblyName != null && _stableAssemblyNames.Contains(assemblyName);
      }

      internal TypeIdentifier GetId(Type type) => _typeToIdentifier.GetOrAdd(type, ComputeId);

      internal Type GetType(TypeIdentifier typeId) => _stringToType.GetOrAdd(typeId.StringRepresentation, _ => typeId.ResolveToType(this));

      internal Type GetTypeFromPersistedString(string persistedTypeString) => _stringToType.GetOrAdd(persistedTypeString, key => TypeIdentifier.Parse(key).ResolveToType(this));

      internal string GetPersistedStringFromAssemblyQualifiedName(string assemblyQualifiedName) =>
         TypeIdentifier.Parse(assemblyQualifiedName).TransformToPersisted(this).StringRepresentation;

      TypeIdentifier ComputeId(Type type)
      {
         // Leaf type with explicit mapping?
         if(_leafTypeToGuid.TryGetValue(type, out var guid))
            return new MappedTypeIdentifier(guid);

         // For composite types (generics, arrays), parse and transform to persisted form
         if(type.IsArray || type is { IsGenericType: true, IsGenericTypeDefinition: false })
         {
            var aqn = type.AssemblyQualifiedName!;
            var parsed = TypeIdentifier.Parse(aqn);
            return parsed.TransformToPersisted(this);
         }

         // Non-mapped, non-composite leaf type — must be from a stable assembly
         var assemblyName = type.Assembly.GetName().Name!;
         if(_stableAssemblyNames.Contains(assemblyName))
         {
            var aqn = type.AssemblyQualifiedName!;
            return TypeIdentifier.Parse(aqn);
         }

         throw new InvalidOperationException(
            $"Type '{type.FullName}' from assembly '{assemblyName}' is not mapped and its assembly is not registered as stable.");
      }

      /// <summary>
      /// Extracts the simple assembly name from a full or simple assembly string.
      /// "System.Private.CoreLib, Version=10.0.0.0, ..." → "System.Private.CoreLib"
      /// "System.Private.CoreLib" → "System.Private.CoreLib"
      /// </summary>
      static string SimpleAssemblyName(string assemblyString)
      {
         var commaIndex = assemblyString.IndexOf(',', StringComparison.Ordinal);
         return commaIndex >= 0 ? assemblyString[..commaIndex].Trim() : assemblyString.Trim();
      }

      Type ITypeMappingLookup.GetLeafType(Guid guid) =>
         _guidToLeafType.TryGetValue(guid, out var type)
            ? type
            : throw new InvalidOperationException($"No type mapping found for GUID: {guid}");

      Type ITypeMappingLookup.GetOpenGenericType(Guid guid) =>
         _guidToOpenGeneric.TryGetValue(guid, out var type)
            ? type
            : throw new InvalidOperationException($"No open generic mapping found for GUID: {guid}");

      bool ITypeMappingLookup.TryGetLeafTypeGuid(Type type, out Guid guid) => _leafTypeToGuid.TryGetValue(type, out guid);
      bool ITypeMappingLookup.TryGetOpenGenericGuid(Type type, out Guid guid) => _openGenericToGuid.TryGetValue(type, out guid);
      bool ITypeMappingLookup.IsStableAssembly(string assemblyName) => _stableAssemblyNames.Contains(SimpleAssemblyName(assemblyName));
   }
}
