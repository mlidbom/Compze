using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Compze.Abstractions.Refactoring.Naming;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>
/// Mutable implementation of <see cref="IStructuralTypeMapper"/> that supports incremental assembly registration.
/// Leaf types get <see cref="MappedTypeId"/> (GUID-backed).
/// Constructed types use structural string representations via <see cref="TypeNameMapper"/>.
/// </summary>
public class StructuralTypeMapper : IStructuralTypeMapper
{
   readonly TypeNameMapper _typeNameMapper = new();
   readonly ConcurrentDictionary<Type, MappedTypeId> _typeToId = new();
   readonly ConcurrentDictionary<MappedTypeId, Type> _idToType = new();
   readonly ConcurrentDictionary<Type, MappedTypeId> _constructedTypeToMappedId = new();
   readonly ConcurrentDictionary<Type, IReadOnlySet<MappedTypeId>> _assignableTypeCache = new();
   readonly HashSet<Assembly> _processedAssemblies = [];

   /// <summary>Well-known Microsoft public key tokens. All assemblies signed with these are stable by default.</summary>
   static readonly HashSet<string> MicrosoftPublicKeyTokenSet =
   [
      "7cec85d7bea7798e", // System.Private.CoreLib
      "b03f5f7f11d50a3a", // most System.* runtime libraries
      "b77a5c561934e089", // legacy (mscorlib, System, System.Core)
      "cc7b13ffcd2ddd51", // System.Private.Xml, netstandard, etc.
      "31bf3856ad364e35"  // Microsoft.* libraries
   ];

   public StructuralTypeMapper()
   {
      // Auto-detect and register all currently-loaded Microsoft assemblies as stable
      foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
         TryRegisterMicrosoftAssemblyAsStable(assembly);
   }

   public void MapTypesFromAssemblyContaining<T>() => MapTypesFromAssembly(typeof(T).Assembly);

   public void MapTypesFromAssembly(Assembly assembly)
   {
      if(!_processedAssemblies.Add(assembly))
         return; // already processed

      var attribute = assembly.GetCustomAttribute<TypeMappingsAttribute>();
      if(attribute == null)
         throw new InvalidOperationException(
            $"Assembly '{assembly.GetName().Name}' does not have a [{nameof(TypeMappingsAttribute)}]. " +
            $"Add [assembly: {nameof(TypeMappingsAttribute)}(typeof(YourMappingClass))] to the assembly.");

      var declarationType = attribute.DeclarationType;
      if(!typeof(ITypeMappingDeclaration).IsAssignableFrom(declarationType))
         throw new InvalidOperationException(
            $"Type '{declarationType.FullName}' specified in [{nameof(TypeMappingsAttribute)}] " +
            $"does not implement {nameof(ITypeMappingDeclaration)}.");

      var declaration = (ITypeMappingDeclaration)Activator.CreateInstance(declarationType)!;
      var registrar = new TypeMappingRegistrar(assembly);
      declaration.DeclareMappings(registrar);

      foreach(var kvp in registrar.LeafTypeMappings)
      {
         var mappedId = new MappedTypeId(kvp.Value);
         _typeToId[kvp.Key] = mappedId;
         _idToType[mappedId] = kvp.Key;
         _typeNameMapper.AddLeafTypeMapping(kvp.Key, kvp.Value);
      }

      foreach(var kvp in registrar.OpenGenericMappings)
      {
         var mappedId = new MappedTypeId(kvp.Value);
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

   public StructuralTypeId GetId(Type type) => _typeNameMapper.GetId(type);

   public Type GetType(StructuralTypeId id) => _typeNameMapper.GetType(id);

   public bool TryGetType(StructuralTypeId id, [NotNullWhen(true)] out Type? type)
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

   public MappedTypeId GetMappedId(Type type)
   {
      if(_typeToId.TryGetValue(type, out var id))
         return id;

      // For constructed types (generics, arrays), derive a deterministic GUID from the structural string
      if(CanResolve(type))
         return _constructedTypeToMappedId.GetOrAdd(type, ComputeDeterministicMappedId);

      throw new InvalidOperationException($"No mapping found for type: {type.FullName}. Ensure the assembly declaring this type has been registered.");
   }

   MappedTypeId ComputeDeterministicMappedId(Type type)
   {
      var structuralId = _typeNameMapper.GetId(type);
      var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(structuralId.StringRepresentation));
      var guidBytes = hash.AsSpan(0, 16).ToArray();
      // Set version nibble to 0x80 (custom/private) to distinguish from leaf-mapped GUIDs
      guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x80);
      var mappedId = new MappedTypeId(new Guid(guidBytes));
      _idToType[mappedId] = type;
      return mappedId;
   }

   public Type GetType(MappedTypeId id)
   {
      if(_idToType.TryGetValue(id, out var type))
         return type;

      throw new InvalidOperationException($"No type found for MappedTypeId: {id}");
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

   IReadOnlySet<MappedTypeId> ComputeAssignableTypeIds(Type baseType)
   {
      var result = new HashSet<MappedTypeId>();

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

   void TryRegisterMicrosoftAssemblyAsStable(Assembly assembly)
   {
      var tokenBytes = assembly.GetName().GetPublicKeyToken();
      if(tokenBytes == null || tokenBytes.Length == 0)
         return;

      var token = Convert.ToHexStringLower(tokenBytes);
      if(MicrosoftPublicKeyTokenSet.Contains(token))
      {
         var name = assembly.GetName().Name;
         if(name != null)
            _typeNameMapper.AddStableAssemblyName(name);
      }
   }

   ///<summary>Temporary bridge: auto-discovers all loaded assemblies with [TypeMappings].
   /// Will be replaced with explicit per-endpoint registration.</summary>
   public void MapTypesFromAllLoadedAssembliesWithTypeMappingsAttribute()
   {
      foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
         if(assembly.GetCustomAttribute<TypeMappingsAttribute>() != null)
            MapTypesFromAssembly(assembly);
      }
   }
}
