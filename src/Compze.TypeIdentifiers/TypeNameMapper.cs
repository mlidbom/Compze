using System.Collections.Concurrent;
using Compze.TypeIdentifiers.Parsing;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Transforms between .NET <see cref="Type"/> objects and <see cref="TypeIdentifier"/> values.
/// Uses <see cref="Parsing.ParsedTypeName"/> for string manipulation and mapping dictionaries for GUID↔Type lookups.
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
      _leafTypeToGuid[type] = guid;
      _guidToLeafType[guid] = type;
      ClearCaches();
   }

   internal void AddOpenGenericMapping(Type openGenericType, Guid guid)
   {
      _openGenericToGuid[openGenericType] = guid;
      _guidToOpenGeneric[guid] = openGenericType;
      ClearCaches();
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
      var parsed = ParsedTypeName.Parse(key);
      return ResolveFromParsed(parsed);
   });

   /// <summary>
   /// Given a raw <c>AssemblyQualifiedName</c> from Newtonsoft (serialize direction),
   /// produce the persisted string with mapped components replaced by GUIDs.
   /// </summary>
   internal string GetPersistedStringFromAssemblyQualifiedName(string assemblyQualifiedName)
   {
      var parsed = ParsedTypeName.Parse(assemblyQualifiedName);
      var transformed = TransformToPersisted(parsed);
      return transformed.ToAssemblyQualifiedNameString();
   }

   TypeIdentifier ComputeId(Type type)
   {
      // Leaf type with explicit mapping?
      if(_leafTypeToGuid.TryGetValue(type, out var guid))
         return new MappedTypeIdentifier(guid);

      // For composite types (generics, arrays), build the structural string
      if(type.IsArray || type is { IsGenericType: true, IsGenericTypeDefinition: false })
      {
         var aqn = type.AssemblyQualifiedName!;
         var parsed = ParsedTypeName.Parse(aqn);
         var transformed = TransformToPersisted(parsed);
         var resultString = transformed.ToAssemblyQualifiedNameString();

         // If nothing was transformed (all stable), it's a StableNameTypeId
         if(resultString == aqn)
            return new StableNameTypeIdentifier(aqn);

         return new ConstructedTypeIdentifier(resultString);
      }

      // Non-mapped, non-composite leaf type — must be from a stable assembly
      var assemblyName = type.Assembly.GetName().Name!;
      if(_stableAssemblyNames.Contains(assemblyName))
         return new StableNameTypeIdentifier(type.AssemblyQualifiedName!);

      throw new InvalidOperationException(
         $"Type '{type.FullName}' from assembly '{assemblyName}' is not mapped and its assembly is not registered as stable.");
   }

   Type ResolveType(TypeIdentifier typeId) => typeId switch
   {
      MappedTypeIdentifier mapped => _guidToLeafType.TryGetValue(mapped.GuidValue, out var type)
         ? type
         : throw new InvalidOperationException($"No type mapping found for GUID: {mapped.GuidValue}"),

      StableNameTypeIdentifier stable => Type.GetType(stable.StringRepresentation)
         ?? throw new InvalidOperationException($"Could not resolve stable type: {stable.StringRepresentation}"),

      ConstructedTypeIdentifier constructed => GetTypeFromPersistedString(constructed.StringRepresentation),

      _ => throw new ArgumentOutOfRangeException(nameof(typeId), $"Unknown TypeId subtype: {typeId.GetType().Name}")
   };

   Type ResolveFromParsed(ParsedTypeName parsed) => parsed.ResolveToType(this);

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

   ParsedTypeName TransformToPersisted(ParsedTypeName parsed) => parsed.TransformToPersisted(this);

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
