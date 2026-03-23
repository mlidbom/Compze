using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Underscore;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Mutable implementation of <see cref="ITypeMapper"/> that supports incremental assembly registration.
/// Leaf types get <see cref="MappedTypeIdentifier"/> (GUID-backed).
/// Constructed types use structural string representations via <see cref="TypeNameMapper"/>.
/// </summary>
public class TypeMapper : ITypeMapper
{
   readonly TypeNameMapper _typeNameMapper = new();
   readonly ConcurrentDictionary<Type, MappedTypeIdentifier> _typeToId = new();
   readonly ConcurrentDictionary<MappedTypeIdentifier, Type> _idToType = new();
   readonly ConcurrentDictionary<Type, MappedTypeIdentifier> _constructedTypeToMappedId = new();
   readonly ConcurrentDictionary<Type, IReadOnlySet<MappedTypeIdentifier>> _assignableTypeCache = new();
   readonly HashSet<Assembly> _processedAssemblies = [];

   /// <summary>Well-known Microsoft public key tokens. All assemblies signed with these are stable by default.</summary>
   static readonly HashSet<string> MicrosoftPublicKeyTokens = ["7cec85d7bea7798e", "b03f5f7f11d50a3a", "b77a5c561934e089", "cc7b13ffcd2ddd51", "31bf3856ad364e35"];

   public TypeMapper() => RegisterMicrosoftAssembliesAsStableNameAssemblies();

   public void MapTypesFromAssemblyContaining<T>() => MapTypesFromAssembly(typeof(T).Assembly);

   public void MapTypesFromAssembly(Assembly assembly)
   {
      if(!_processedAssemblies.Add(assembly))
         return; // already processed

      var attribute = assembly.GetCustomAttribute<AssemblyTypeMapperAttribute>();
      if(attribute == null)
         throw new InvalidOperationException(
            $"Assembly '{assembly.GetName().Name}' does not have a [{nameof(AssemblyTypeMapperAttribute)}]. " +
            $"Add [assembly: {nameof(AssemblyTypeMapperAttribute)}(typeof(YourMapper))] to the assembly.");

      var mapperType = attribute.Mapper;
      if(!typeof(IAssemblyTypeMapper).IsAssignableFrom(mapperType))
         throw new InvalidOperationException(
            $"Type '{mapperType.FullName}' specified in [{nameof(AssemblyTypeMapperAttribute)}] " +
            $"does not implement {nameof(IAssemblyTypeMapper)}.");

      var mapper = (IAssemblyTypeMapper)Activator.CreateInstance(mapperType)!;
      var registrar = new TypeMappingRegistrar(assembly);
      mapper.Map(registrar);

      foreach(var kvp in registrar.LeafTypeMappings)
      {
         var mappedId = new MappedTypeIdentifier(kvp.Value);
         _typeToId[kvp.Key] = mappedId;
         _idToType[mappedId] = kvp.Key;
         _typeNameMapper.AddLeafTypeMapping(kvp.Key, kvp.Value);
      }

      foreach(var kvp in registrar.OpenGenericMappings)
      {
         var mappedId = new MappedTypeIdentifier(kvp.Value);
         _typeToId[kvp.Key] = mappedId;
         _idToType[mappedId] = kvp.Key;
         _typeNameMapper.AddOpenGenericMapping(kvp.Key, kvp.Value);
      }

      _assignableTypeCache.Clear();
      _constructedTypeToMappedId.Clear();
   }

   public void UseStableNameStrategyForAssemblyContaining<T>()
   {
      var name = typeof(T).Assembly.GetName().Name;
      if(name != null)
         _typeNameMapper.AddStableAssemblyName(name);

      _assignableTypeCache.Clear();
      _constructedTypeToMappedId.Clear();
   }

   public TypeIdentifier GetId(Type type) => _typeNameMapper.GetId(type);

   public Type GetType(TypeIdentifier id) => _typeNameMapper.GetType(id);

   public bool TryGetType(TypeIdentifier id, [NotNullWhen(true)] out Type? type)
   {
      try
      {
         type = _typeNameMapper.GetType(id);
         return true;
      }
      catch(InvalidOperationException)
      {
         type = null;
         return false;
      }
   }

   public MappedTypeIdentifier GetMappedId(Type type)
   {
      if(_typeToId.TryGetValue(type, out var id))
         return id;

      // For constructed types (generics, arrays), derive a deterministic GUID from the structural string
      if(CanResolve(type))
         return _constructedTypeToMappedId.GetOrAdd(type, ComputeDeterministicMappedId);

      throw new InvalidOperationException($"No mapping found for type: {type.FullName}. Ensure the assembly declaring this type has been registered.");
   }

   MappedTypeIdentifier ComputeDeterministicMappedId(Type type)
   {
      var structuralId = _typeNameMapper.GetId(type);
      var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(structuralId.StringRepresentation));
      var guidBytes = hash.AsSpan(0, 16).ToArray();
      // Set version nibble to 0x80 (custom/private) to distinguish from leaf-mapped GUIDs
      guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x80);
      var mappedId = new MappedTypeIdentifier(new Guid(guidBytes));
      _idToType[mappedId] = type;
      return mappedId;
   }

   public Type GetType(MappedTypeIdentifier id)
   {
      if(_idToType.TryGetValue(id, out var type))
         return type;

      throw new InvalidOperationException($"No type found for MappedTypeIdentifier: {id}");
   }

   public IEnumerable<MappedTypeIdentifier> GetIdForTypesAssignableTo(Type type)
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
         return _typeNameMapper.HasMappingForOpenGeneric(type.GetGenericTypeDefinition())
             && type.GetGenericArguments().All(CanResolve);

      if(type.IsArray)
         return CanResolve(type.GetElementType()!);

      return _typeNameMapper.IsStableType(type);
   }

   public string ToPersistedTypeString(Type type)
      => _typeNameMapper.GetPersistedStringFromAssemblyQualifiedName(type.AssemblyQualifiedName!);

   public Type FromPersistedTypeString(string persistedTypeString)
      => _typeNameMapper.GetTypeFromPersistedString(persistedTypeString);

   IReadOnlySet<MappedTypeIdentifier> ComputeAssignableTypeIds(Type baseType)
   {
      var result = new HashSet<MappedTypeIdentifier>();

      foreach(var kvp in _typeToId)
      {
         if(baseType.IsAssignableFrom(kvp.Key))
            result.Add(kvp.Value);
      }

      // Also include constructed types that have been seen (via GetMappedId)
      foreach(var kvp in _constructedTypeToMappedId)
      {
         if(baseType.IsAssignableFrom(kvp.Key))
            result.Add(kvp.Value);
      }

      // If the queried type itself is resolvable but not yet in any cache, add it
      if(result.Count == 0 && CanResolve(baseType))
         result.Add(GetMappedId(baseType));

      return result;
   }

   ///<summary>Temporary bridge: auto-discovers all loaded assemblies with [TypeMappings].
   /// Will be replaced with explicit per-endpoint registration.</summary>
   public void MapTypesFromAllLoadedAssembliesWithTypeMappingsAttribute() =>
      AppDomain.CurrentDomain.GetAssemblies()
               .Where(it => it.GetCustomAttribute<AssemblyTypeMapperAttribute>() != null)
               .ForEach(MapTypesFromAssembly);

   void RegisterMicrosoftAssembliesAsStableNameAssemblies()
   {
      AppDomain.CurrentDomain.GetAssemblies()
               .Where(it => it.PublicKeyTokenString._(MicrosoftPublicKeyTokens.Contains))
               .Select(it => it.SimpleName)
               .WhereNotNull()
               .ForEach(it => _typeNameMapper.AddStableAssemblyName(it));
   }
}
