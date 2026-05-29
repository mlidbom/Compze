using System.Collections.Concurrent;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Transforms between .NET <see cref="Type"/> objects and <see cref="TypeIdentifier"/> values.
/// Supports incremental registration of assemblies. Caches results in both directions. Thread-safe.
/// </summary>
class TypeNameMapper : ITypeMappingLookup
{
   // Leaf type mappings: Type ↔ Guid
   readonly ConcurrentDictionary<Guid, Type> _guidToLeafType = new();
   readonly ConcurrentDictionary<Type, Guid> _leafTypeToGuid = new();

   // Open generic mappings: Type ↔ Guid
   readonly ConcurrentDictionary<Guid, Type> _guidToOpenGeneric = new();
   readonly ConcurrentDictionary<Type, Guid> _openGenericToGuid = new();

   // Stable assembly names
   readonly HashSet<string> _stableAssemblyNames = [];
   readonly object _stableAssemblyNamesLock = new();

   // Caches (populated on demand, thread-safe)
   readonly ConcurrentDictionary<Type, TypeIdentifier> _typeToIdentifier = new();
   readonly ConcurrentDictionary<string, Type> _stringToType = new();

   internal void AddLeafTypeMapping(Type type, Guid guid)
   {
      AssertTypeAndGuidAreUnmapped(type, guid, _leafTypeToGuid, _guidToLeafType);
      _leafTypeToGuid[type] = guid;
      _guidToLeafType[guid] = type;
      ClearCaches();
   }

   internal void AddOpenGenericMapping(Type openGenericType, Guid guid)
   {
      AssertTypeAndGuidAreUnmapped(openGenericType, guid, _openGenericToGuid, _guidToOpenGeneric);
      _openGenericToGuid[openGenericType] = guid;
      _guidToOpenGeneric[guid] = openGenericType;
      ClearCaches();
   }

   // A type↔GUID mapping is permanent identity. Re-mapping a type, or binding a GUID to a second type, silently
   // corrupts the reverse lookup and makes already-persisted data resolve to the wrong type. This is the funnel
   // every registration passes through — including across assemblies — so it is where both are rejected.
   static void AssertTypeAndGuidAreUnmapped(Type type, Guid guid, ConcurrentDictionary<Type, Guid> typeToGuid, ConcurrentDictionary<Guid, Type> guidToType)
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

   internal void AddStableAssemblyName(string assemblyName)
   {
      lock(_stableAssemblyNamesLock)
      {
         _stableAssemblyNames.Add(assemblyName);
      }

      ClearCaches();
   }

   void ClearCaches()
   {
      _typeToIdentifier.Clear();
      _stringToType.Clear();
   }

   internal bool HasMappingForOpenGeneric(Type openGenericType) => _openGenericToGuid.ContainsKey(openGenericType);

   /// <summary>All leaf types that have an explicit GUID mapping.</summary>
   internal IEnumerable<Type> RegisteredLeafTypes => _leafTypeToGuid.Keys;

   internal bool HasLeafMapping(Type type) => _leafTypeToGuid.ContainsKey(type);

   internal bool IsStableType(Type type)
   {
      var assemblyName = type.Assembly.GetName().Name;
      lock(_stableAssemblyNamesLock)
      {
         return assemblyName != null && _stableAssemblyNames.Contains(assemblyName);
      }
   }

   /// <summary>
   /// Given a .NET <see cref="Type"/>, produce the correct <see cref="TypeIdentifier"/> subtype.
   /// </summary>
   internal TypeIdentifier GetId(Type type) => _typeToIdentifier.GetOrAdd(type, ComputeId);

   /// <summary>
   /// Given a <see cref="TypeIdentifier"/>, resolve it back to a .NET <see cref="Type"/>.
   /// </summary>
   internal Type GetType(TypeIdentifier typeId) => _stringToType.GetOrAdd(typeId.StringRepresentation, _ => ResolveType(typeId));

   /// <summary>
   /// Given a string from a persisted <c>$type</c> field, resolve it to a .NET <see cref="Type"/>.
   /// This is the deserialization entry point — the string is parsed, then resolved.
   /// </summary>
   internal Type GetTypeFromPersistedString(string persistedTypeString) => _stringToType.GetOrAdd(persistedTypeString, key =>
   {
      var parsed = TypeIdentifier.Parse(key);
      return parsed.ResolveToType(this);
   });

   /// <summary>
   /// Given a raw <c>AssemblyQualifiedName</c> from Newtonsoft (serialize direction),
   /// produce the persisted string with mapped components replaced by GUIDs.
   /// </summary>
   internal string GetPersistedStringFromAssemblyQualifiedName(string assemblyQualifiedName)
   {
      var parsed = TypeIdentifier.Parse(assemblyQualifiedName);
      var transformed = parsed.TransformToPersisted(this);
      return transformed.StringRepresentation;
   }

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

   Type ResolveType(TypeIdentifier typeId) => typeId.ResolveToType(this);

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

   bool ITypeMappingLookup.IsStableAssembly(string assemblyName)
   {
      lock(_stableAssemblyNamesLock)
      {
         return _stableAssemblyNames.Contains(SimpleAssemblyName(assemblyName));
      }
   }
}
