using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Compze.Abstractions.Refactoring.Naming;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>
/// Standalone implementation of <see cref="IStructuralTypeMapper"/> that gets all mapping data
/// from <see cref="TypeMappingsAttribute"/> declarations on assemblies.
/// Does not depend on the old <see cref="TypeMapper"/> infrastructure at all.
/// </summary>
class StructuralTypeMapper : IStructuralTypeMapper
{
   readonly TypeNameMapper _typeNameMapper;
   readonly Dictionary<Type, MappedTypeId> _typeToId;
   readonly Dictionary<MappedTypeId, Type> _idToType;
   readonly ConcurrentDictionary<Type, IReadOnlySet<MappedTypeId>> _assignableTypeCache = new();

   StructuralTypeMapper(TypeNameMapper typeNameMapper, Dictionary<Type, MappedTypeId> typeToId, Dictionary<MappedTypeId, Type> idToType)
   {
      _typeNameMapper = typeNameMapper;
      _typeToId = typeToId;
      _idToType = idToType;
   }

   public MappedTypeId GetId(Type type)
   {
      if(_typeToId.TryGetValue(type, out var id))
         return id;
      throw new InvalidOperationException($"No mapping found for type: {type.FullName}");
   }

   public Type GetType(MappedTypeId id)
   {
      if(_idToType.TryGetValue(id, out var type))
         return type;
      throw new InvalidOperationException($"No type found for MappedTypeId: {id}");
   }

   public bool TryGetType(MappedTypeId id, [NotNullWhen(true)] out Type? type)
      => _idToType.TryGetValue(id, out type);

   public IEnumerable<MappedTypeId> GetIdForTypesAssignableTo(Type type)
      => _assignableTypeCache.GetOrAdd(type, ComputeAssignableTypeIds);

   public void AssertMappingsExistFor(IEnumerable<Type> types)
   {
      var missing = types.Where(type => !_typeToId.ContainsKey(type)).ToList();
      if(missing.Count > 0)
         throw new InvalidOperationException(
            $"Missing type mappings for: {string.Join(", ", missing.Select(t => t.FullName))}");
   }

   public string ToPersistedTypeString(Type type)
      => _typeNameMapper.GetPersistedStringFromAssemblyQualifiedName(type.AssemblyQualifiedName!);

   public Type FromPersistedTypeString(string persistedTypeString)
      => _typeNameMapper.GetTypeFromPersistedString(persistedTypeString);

   IReadOnlySet<MappedTypeId> ComputeAssignableTypeIds(Type baseType)
   {
      var result = new HashSet<MappedTypeId>();
      foreach(var kvp in _typeToId)
      {
         if(baseType.IsAssignableFrom(kvp.Key))
            result.Add(kvp.Value);
      }
      return result;
   }

   /// <summary>
   /// Creates a <see cref="StructuralTypeMapper"/> from a set of assemblies that have
   /// <see cref="TypeMappingsAttribute"/> declarations.
   /// </summary>
   internal static StructuralTypeMapper BuildFromAssemblies(params Assembly[] assemblies)
   {
      var builder = new TypeNameMapperBuilder();
      foreach(var assembly in assemblies)
         builder.MapTypesFromAssembly(assembly);

      var typeNameMapper = builder.Build();

      // Build the leaf-type GUID dictionaries from the builder's registrar data.
      // Re-collect from the assemblies to get the leaf mappings.
      var typeToId = new Dictionary<Type, MappedTypeId>();
      var idToType = new Dictionary<MappedTypeId, Type>();

      foreach(var assembly in assemblies)
      {
         var attribute = assembly.GetCustomAttribute<TypeMappingsAttribute>()!;
         var declaration = (ITypeMappingDeclaration)Activator.CreateInstance(attribute.DeclarationType)!;
         var registrar = new TypeMappingRegistrar(assembly);
         declaration.DeclareMappings(registrar);

         foreach(var kvp in registrar.LeafTypeMappings)
         {
            var mappedId = new MappedTypeId(kvp.Value);
            typeToId[kvp.Key] = mappedId;
            idToType[mappedId] = kvp.Key;
         }
      }

      return new StructuralTypeMapper(typeNameMapper, typeToId, idToType);
   }
}
