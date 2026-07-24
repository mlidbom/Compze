using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Compze.TypeIdentifiers._private;

namespace Compze.TypeIdentifiers._private;

/// <summary>
/// The finished, immutable <see cref="ITypeMap"/> — built once by <see cref="TypeMapBuilder"/> from the complete set of
/// assembly declarations. Leaf types and open generic definitions resolve to the GUIDs their assembly declared;
/// constructed and stable types resolve structurally through <see cref="TypeNameMapper"/>. The produced
/// <see cref="TypeId"/> is always canonical for its type.
/// </summary>
sealed class TypeMap(TypeNameMapper typeNameMapper) : ITypeMap
{
   readonly TypeNameMapper _typeNameMapper = typeNameMapper;

   // Never invalidated: the mappings are fixed for this map's whole life, so a cached id can never go stale.
   readonly ConcurrentDictionary<Type, TypeId> _idCache = new();

   /// <inheritdoc />
   public TypeId GetId(Type type) => _idCache.GetOrAdd(type, it => new TypeId(it, _typeNameMapper.GetId(it).StringRepresentation));

   // Resolve the string to its .NET type first, then route through GetId(Type) so the same mapped-or-stable rule
   // applies: a string that resolves to a runtime type with no registered identity is rejected, not silently
   // handed back a type that could never be re-serialized. The resulting TypeId carries both the Type and the
   // canonical string.
   /// <inheritdoc />
   public TypeId GetId(string persistedTypeString) => GetId(_typeNameMapper.GetTypeFromPersistedString(persistedTypeString));

   /// <inheritdoc />
   public bool TryGetId(Type type, [NotNullWhen(true)] out TypeId? id)
   {
      if(!CanResolve(type))
      {
         id = null;
         return false;
      }

      id = GetId(type);
      return true;
   }

   /// <inheritdoc />
   public void AssertMappingsExistFor(IEnumerable<Type> types)
   {
      var missing = types.Where(type => !CanResolve(type)).ToList();
      if(missing.Count > 0)
         throw new InvalidOperationException(
            $"Missing type mappings for: {string.Join(", ", missing.Select(it => it.FullName))}");
   }

   bool CanResolve(Type type)
   {
      if(_typeNameMapper.HasLeafMapping(type))
         return true;

      if(type.IsConstructedGenericType)
         return _typeNameMapper.HasMappingForOpenGeneric(type.GetGenericTypeDefinition())
             && type.GetGenericArguments().All(CanResolve);

      if(type.IsArray)
         return CanResolve(type.GetElementType()!);

      return _typeNameMapper.IsStableType(type);
   }
}
