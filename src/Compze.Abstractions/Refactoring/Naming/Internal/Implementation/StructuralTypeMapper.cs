using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Compze.Abstractions.Refactoring.Naming;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

public static class StructuralTypeMapperRegistrar
{
   public static IComponentRegistrar StructuralTypeMapper(this IComponentRegistrar @this)
      => Implementation.StructuralTypeMapper.RegisterWith(@this);

   public static IStructuralTypeMapper CreateFromLoadedAssemblies()
      => Implementation.StructuralTypeMapper.BuildFromLoadedAssemblies();
}

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
   readonly Dictionary<Type, MappedTypeId> _openGenericMappings;
   readonly ConcurrentDictionary<Type, MappedTypeId> _constructedTypeCache = new();
   readonly ConcurrentDictionary<MappedTypeId, Type> _constructedReverseCache = new();
   readonly ConcurrentDictionary<Type, IReadOnlySet<MappedTypeId>> _assignableTypeCache = new();

   StructuralTypeMapper(TypeNameMapper typeNameMapper, Dictionary<Type, MappedTypeId> typeToId, Dictionary<MappedTypeId, Type> idToType, Dictionary<Type, MappedTypeId> openGenericMappings)
   {
      _typeNameMapper = typeNameMapper;
      _typeToId = typeToId;
      _idToType = idToType;
      _openGenericMappings = openGenericMappings;
   }

   public MappedTypeId GetId(Type type)
   {
      if(_typeToId.TryGetValue(type, out var id))
         return id;

      return _constructedTypeCache.GetOrAdd(type, ResolveConstructedTypeId);
   }

   MappedTypeId ResolveConstructedTypeId(Type type)
   {
      MappedTypeId result;
      if(type.IsConstructedGenericType)
      {
         var openGenericType = type.GetGenericTypeDefinition();
         if(!_openGenericMappings.TryGetValue(openGenericType, out var openGenericId)
            && !_typeToId.TryGetValue(openGenericType, out openGenericId))
            throw new InvalidOperationException($"No mapping found for open generic type: {openGenericType.FullName}");

         var typeArgs = type.GetGenericArguments();
         var argIds = new Guid[typeArgs.Length];
         for(var i = 0; i < typeArgs.Length; i++)
            argIds[i] = GetId(typeArgs[i]).GuidValue;

         var componentGuids = argIds.Prepend(openGenericId.GuidValue).ToList();
         var compositeGuid = Guid.NewUUIDv5(namespaceId: CompositionNamespaceId, components: componentGuids);
         result = new MappedTypeId(compositeGuid);
      }
      else if(type.IsArray)
      {
         var elementId = GetId(type.GetElementType()!);
         var compositeGuid = Guid.NewUUIDv5(namespaceId: ArrayMarkerGuid,
                                            components: [elementId.GuidValue, Guid.NewUUIDv5(namespaceId: ArrayMarkerGuid, name: type.GetArrayRank().ToString(System.Globalization.CultureInfo.InvariantCulture))]);
         result = new MappedTypeId(compositeGuid);
      }
      else
      {
         throw new InvalidOperationException($"No mapping found for type: {type.FullName}");
      }

      _constructedReverseCache.TryAdd(result, type);
      return result;
   }

   // Must match DeterministicTypeId.CompositionNamespaceId exactly — persisted data depends on it.
   static readonly Guid CompositionNamespaceId = new("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
   static readonly Guid ArrayMarkerGuid = new("b7e3d8f1-6a2c-4e0b-8d5f-1c9a4b3e2d6f");

   public Type GetType(MappedTypeId id)
   {
      if(_idToType.TryGetValue(id, out var type))
         return type;
      if(_constructedReverseCache.TryGetValue(id, out type))
         return type;
      throw new InvalidOperationException($"No type found for MappedTypeId: {id}");
   }

   public bool TryGetType(MappedTypeId id, [NotNullWhen(true)] out Type? type)
   {
      if(_idToType.TryGetValue(id, out type))
         return true;
      return _constructedReverseCache.TryGetValue(id, out type);
   }

   public IEnumerable<MappedTypeId> GetIdForTypesAssignableTo(Type type)
      => _assignableTypeCache.GetOrAdd(type, ComputeAssignableTypeIds);

   public void AssertMappingsExistFor(IEnumerable<Type> types)
   {
      var missing = types.Where(type => !CanResolve(type)).ToList();
      if(missing.Count > 0)
         throw new InvalidOperationException(
            $"Missing type mappings for: {string.Join(", ", missing.Select(t => t.FullName))}");
   }

   bool CanResolve(Type type)
   {
      if(_typeToId.ContainsKey(type))
         return true;

      if(type.IsConstructedGenericType)
      {
         var openGenericType = type.GetGenericTypeDefinition();
         if(!_openGenericMappings.ContainsKey(openGenericType) && !_typeToId.ContainsKey(openGenericType))
            return false;
         return type.GetGenericArguments().All(CanResolve);
      }

      if(type.IsArray)
         return CanResolve(type.GetElementType()!);

      return false;
   }

   public string ToPersistedTypeString(Type type)
      => _typeNameMapper.GetPersistedStringFromAssemblyQualifiedName(type.AssemblyQualifiedName!);

   public Type FromPersistedTypeString(string persistedTypeString)
      => _typeNameMapper.GetTypeFromPersistedString(persistedTypeString);

   IReadOnlySet<MappedTypeId> ComputeAssignableTypeIds(Type baseType)
   {
      var result = new HashSet<MappedTypeId>();

      // Resolve the requested type itself if it's a constructed generic/array —
      // the old TypeMapper did this via assembly scanning; we do it on-demand.
      if(CanResolve(baseType) && !_typeToId.ContainsKey(baseType))
      {
         var id = GetId(baseType);
         result.Add(id);
      }

      foreach(var kvp in _typeToId)
      {
         if(baseType.IsAssignableFrom(kvp.Key))
            result.Add(kvp.Value);
      }
      return result;
   }

   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IStructuralTypeMapper>()
                                     .CreatedBy(BuildFromLoadedAssemblies));

   /// <summary>
   /// Creates a <see cref="StructuralTypeMapper"/> from all currently loaded assemblies
   /// that have <see cref="TypeMappingsAttribute"/> declarations.
   /// </summary>
   internal static StructuralTypeMapper BuildFromLoadedAssemblies()
   {
      var assembliesWithMappings = AppDomain.CurrentDomain.GetAssemblies()
                                            .Where(assembly => assembly.GetCustomAttribute<TypeMappingsAttribute>() != null)
                                            .ToArray();

      var allAssembliesToScan = AppDomain.CurrentDomain.GetAssemblies()
                                         .Where(TypeMapperAssemblyScanner.IsAssemblyWeShouldExamine)
                                         .ToArray();

      return BuildFromAssemblies(assembliesWithMappings, allAssembliesToScan);
   }

   internal static StructuralTypeMapper BuildFromAssemblies(Assembly[] assembliesWithMappings, Assembly[]? assembliesToScanForConstructedTypes = null)
   {
      var builder = new TypeNameMapperBuilder();
      foreach(var assembly in assembliesWithMappings)
         builder.MapTypesFromAssembly(assembly);

      var typeNameMapper = builder.Build();

      // Build the leaf-type GUID dictionaries from the builder's registrar data.
      // Re-collect from the assemblies to get the leaf mappings.
      var typeToId = new Dictionary<Type, MappedTypeId>();
      var idToType = new Dictionary<MappedTypeId, Type>();
      var openGenericMappings = new Dictionary<Type, MappedTypeId>();

      foreach(var assembly in assembliesWithMappings)
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

         foreach(var kvp in registrar.OpenGenericMappings)
         {
            openGenericMappings[kvp.Key] = new MappedTypeId(kvp.Value);
         }
      }

      var mapper = new StructuralTypeMapper(typeNameMapper, typeToId, idToType, openGenericMappings);

      // Pre-scan assemblies for constructed generic types so their IDs are in the reverse cache.
      // This is needed so that remote endpoints can resolve MappedTypeIds back to Types.
      var scanAssemblies = assembliesToScanForConstructedTypes ?? assembliesWithMappings;
      foreach(var assembly in scanAssemblies)
      {
         if(!TypeMapperAssemblyScanner.IsAssemblyWeShouldExamine(assembly))
            continue;

         var scannedTypes = TypeMapperAssemblyScanner.Scan(assembly);
         foreach(var computedType in scannedTypes.ComputedTypeIdTypes)
         {
            if(mapper.CanResolve(computedType.Type) && !mapper._typeToId.ContainsKey(computedType.Type))
            {
               var id = mapper.GetId(computedType.Type);
               mapper._typeToId[computedType.Type] = id;
               mapper._idToType[id] = computedType.Type;
            }
         }
      }

      return mapper;
   }
}
