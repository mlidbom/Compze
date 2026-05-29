using System.Collections.Concurrent;
using System.Reflection;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Underscore;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Mutable implementation of <see cref="ITypeMapper"/> and <see cref="ITypeMap"/> that supports incremental
/// assembly registration. Leaf types and open generic definitions are mapped to GUIDs; constructed and
/// stable types resolve structurally via <see cref="TypeNameMapper"/>. The produced <see cref="TypeId"/>
/// is always canonical for its type.
/// </summary>
public class TypeMapper : ITypeMapper, ITypeMap
{
   readonly TypeNameMapper _typeNameMapper = new();
   volatile Caches _caches = new();
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
      var registrar = new AssemblyTypeMappingRegistrar(assembly);
      mapper.Map(registrar);

      foreach(var kvp in registrar.LeafTypeMappings)
         _typeNameMapper.AddLeafTypeMapping(kvp.Key, kvp.Value);

      foreach(var kvp in registrar.OpenGenericMappings)
         _typeNameMapper.AddOpenGenericMapping(kvp.Key, kvp.Value);

      ClearCaches();
   }

   public void UseStableNameStrategyForAssemblyContaining<T>()
   {
      var name = typeof(T).Assembly.GetName().Name;
      if(name != null)
         _typeNameMapper.AddStableAssemblyName(name);

      ClearCaches();
   }

   public TypeId GetId(Type type) => _caches.IdCache.GetOrAdd(type, t => new TypeId(t, _typeNameMapper.GetId(t).StringRepresentation));

   public string ToPersistedTypeString(Type type) => GetId(type).CanonicalString;

   public Type FromPersistedTypeString(string persistedTypeString) => _typeNameMapper.GetTypeFromPersistedString(persistedTypeString);

   public TypeId GetIdFromPersistedString(string persistedTypeString) => GetId(FromPersistedTypeString(persistedTypeString));

   public void AssertMappingsExistFor(IEnumerable<Type> types)
   {
      var missing = types.Where(type => !CanResolve(type)).ToList();
      if(missing.Count > 0)
         throw new InvalidOperationException(
            $"Missing type mappings for: {string.Join(", ", missing.Select(t => t.FullName))}");
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

   ///<summary>Temporary bridge: auto-discovers all loaded assemblies with [TypeMappings].
   /// Will be replaced with explicit per-endpoint registration.</summary>
   public void MapTypesFromAllLoadedAssembliesWithTypeMappingsAttribute() =>
      AppDomain.CurrentDomain.GetAssemblies()
               .Where(it => it.GetCustomAttribute<AssemblyTypeMapperAttribute>() != null)
               .ForEach(MapTypesFromAssembly);

   // Registration swaps in a fresh, empty cache set rather than clearing in place. A lookup that is mid-flight
   // when this runs stores its result into the now-abandoned previous set, so it can never poison the current one.
   void ClearCaches() => _caches = new Caches();

   sealed class Caches
   {
      internal readonly ConcurrentDictionary<Type, TypeId> IdCache = new();
   }

   void RegisterMicrosoftAssembliesAsStableNameAssemblies()
   {
      AppDomain.CurrentDomain.GetAssemblies()
               .Where(it => it.PublicKeyTokenString._(MicrosoftPublicKeyTokens.Contains))
               .Select(it => it.SimpleName)
               .WhereNotNull()
               .ForEach(it => _typeNameMapper.AddStableAssemblyName(it));
   }
}
