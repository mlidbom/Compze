using Compze.Internals.SystemCE.ReflectionCE;
using System.Collections.Concurrent;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Transforms between .NET <see cref="Type"/> objects and <see cref="TypeIdentifier"/> values, against a fixed set of
/// type mappings. Caches results in both directions.
/// </summary>
/// <remarks>
/// Immutable, and therefore thread-safe with no locking and no cache invalidation: the mappings are complete when the
/// instance is constructed — <see cref="TypeMapBuilder"/> collects and validates every declaration first — so a cached
/// lookup can never be made stale by a later registration. There is no later registration.
/// </remarks>
sealed class TypeNameMapper : ITypeMappingLookup
{
   readonly Dictionary<Guid, Type> _guidToLeafType;
   readonly Dictionary<Type, Guid> _leafTypeToGuid;
   readonly Dictionary<Guid, Type> _guidToOpenGeneric;
   readonly Dictionary<Type, Guid> _openGenericToGuid;
   readonly HashSet<string> _stableAssemblyNames;
   readonly HashSet<string> _stablePublicKeyTokens;

   readonly ConcurrentDictionary<Type, TypeIdentifier> _typeToIdentifier = new();
   readonly ConcurrentDictionary<string, Type> _stringToType = new();

   internal TypeNameMapper(Dictionary<Guid, Type> guidToLeafType,
                           Dictionary<Type, Guid> leafTypeToGuid,
                           Dictionary<Guid, Type> guidToOpenGeneric,
                           Dictionary<Type, Guid> openGenericToGuid,
                           HashSet<string> stableAssemblyNames,
                           HashSet<string> stablePublicKeyTokens)
   {
      _guidToLeafType = guidToLeafType;
      _leafTypeToGuid = leafTypeToGuid;
      _guidToOpenGeneric = guidToOpenGeneric;
      _openGenericToGuid = openGenericToGuid;
      _stableAssemblyNames = stableAssemblyNames;
      _stablePublicKeyTokens = stablePublicKeyTokens;
   }

   internal bool HasMappingForOpenGeneric(Type openGenericType) => _openGenericToGuid.ContainsKey(openGenericType);
   internal bool HasLeafMapping(Type type) => _leafTypeToGuid.ContainsKey(type);

   // A type is rename-safe ("stable") when its assembly is signed with a public key token we trust to keep its
   // type names (the framework tokens, plus any the composition declares), or when the assembly was declared stable
   // by name. Both are read from the live type at lookup time, so an assembly loaded after construction is still
   // recognised — there is no construction-time snapshot of loaded assemblies to go stale.
   internal bool IsStableType(Type type) =>
      _stablePublicKeyTokens.Contains(type.Assembly.PublicKeyTokenString)
   || _stableAssemblyNames.Contains(type.Assembly.SimpleName ?? "");

   /// <summary>Given a .NET <see cref="Type"/>, produce the correct <see cref="TypeIdentifier"/> subtype.</summary>
   internal TypeIdentifier GetId(Type type) => _typeToIdentifier.GetOrAdd(type, ComputeId);

   /// <summary>Given a <see cref="TypeIdentifier"/>, resolve it back to a .NET <see cref="Type"/>.</summary>
   internal Type GetType(TypeIdentifier typeId) => _stringToType.GetOrAdd(typeId.StringRepresentation, _ => typeId.ResolveToType(this));

   /// <summary>
   /// Given a string from a persisted <c>$type</c> field, resolve it to a .NET <see cref="Type"/>.
   /// This is the deserialization entry point — the string is parsed, then resolved.
   /// </summary>
   internal Type GetTypeFromPersistedString(string persistedTypeString) => _stringToType.GetOrAdd(persistedTypeString, key => TypeIdentifier.Parse(key).ResolveToType(this));

   /// <summary>
   /// Given a raw <c>AssemblyQualifiedName</c>, produce the persisted string with mapped components replaced by GUIDs —
   /// the serialize direction of <see cref="GetTypeFromPersistedString"/>.
   /// </summary>
   internal string GetPersistedStringFromAssemblyQualifiedName(string assemblyQualifiedName) =>
      TypeIdentifier.Parse(assemblyQualifiedName).TransformToPersisted(this).StringRepresentation;

   TypeIdentifier ComputeId(Type type)
   {
      if(_leafTypeToGuid.TryGetValue(type, out var guid))
         return new MappedTypeIdentifier(guid);

      // Composite types (generics, arrays) are parsed and transformed component by component.
      if(type.IsArray || type is {IsGenericType: true, IsGenericTypeDefinition: false})
         return TypeIdentifier.Parse(type.AssemblyQualifiedName!).TransformToPersisted(this);

      if(IsStableType(type))
         return TypeIdentifier.Parse(type.AssemblyQualifiedName!);

      throw new InvalidOperationException(
         $"Type '{type.FullName}' from assembly '{type.Assembly.GetName().Name}' is not mapped and its assembly is not registered as stable.");
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
   bool ITypeMappingLookup.IsStableType(Type type) => IsStableType(type);
}
