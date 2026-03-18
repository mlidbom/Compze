using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Threading.ResourceAccess;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

public static class TypeMapperRegistrar
{
   public static IComponentRegistrar TypeMapper(this IComponentRegistrar @this)
      => Implementation.TypeMapper.RegisterWith(@this);
}

public class TypeMapper : ITypeMapper
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<ITypeMapper, TypeMapper>()
                                  .Instance(Instance));

   TypeMapper() {}
   public static readonly ITypeMapper Instance = new TypeMapper();
   static readonly IThreadShared<MappingState> State = IThreadShared.New(new MappingState());

   static TypeMapper()
   {
      EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings();
      AppDomain.CurrentDomain.AssemblyLoad += (_, _) => EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings();
   }

   public TypeId GetId(Type type)
   {
      return State.Locked(state =>
      {
         if(TryGetOrComputeTypeId(type, state, out var typeId))
            return typeId;

         if(state.AssemblyMappingUpdateTessages.TryGetValue(type.Assembly, out var tessage))
            throw new Exception($"Failed to find TypeId for type: {type.FullName}{Environment.NewLine}{tessage}");

         throw MissingMappingReporter.BuildMissingTypesException([type]);
      });
   }

   public Type GetType(TypeId teventTypeId)
   {
      return State.Locked(state =>
      {
         if(state.TypeIdToTypeMap.TryGetValue(teventTypeId, out var type))
            return type;

         throw new Exception($"Could not find type for {nameof(TypeId)}: {teventTypeId}");
      });
   }

   public bool TryGetType(TypeId typeId, [NotNullWhen(true)] out Type? type)
   {
      type = State.Locked(state => state.TypeIdToTypeMap.GetValueOrDefault(typeId));
      return type != null;
   }

   public IEnumerable<TypeId> GetIdForTypesAssignableTo(Type type)
   {
      return State.Locked(state =>
      {
         EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings();

         TryGetOrComputeTypeId(type, state, out _);

         var found = state
                    .TypeToTypeIdMap
                    .Keys
                    .Where(type.IsAssignableFrom)
                    .Select(matchingType => state.TypeToTypeIdMap[matchingType])
                    .ToArray();

         if(!found.Any())
         {
            if(state.AssemblyMappingUpdateTessages.TryGetValue(type.Assembly, out var tessage))
               throw new Exception($"Failed to find TypeIds for types assignable to: {type.FullName}{Environment.NewLine}{tessage}");

            throw MissingMappingReporter.BuildMissingTypesException([type]);
         }

         return found;
      });
   }

   public Unit AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings) => State.Locked(state =>
   {
      var missing = typesThatRequireMappings.Where(type => !TryGetOrComputeTypeId(type, state, out _)).ToList();
      if(missing.Any()) throw MissingMappingReporter.BuildMissingTypesException(missing);
   });

   static void AssertTypeValidForMapping(Type type)
   {
      if(type.IsGenericTypeDefinition) return;

      if(type.IsAbstract)
      {
         if(!typeof(IRemotableTevent).IsAssignableFrom(type))
            throw new Exception($"Type: {type.FullName} is abstract and is not a {typeof(IRemotableTevent).FullName}. For other types you should only map concrete types.");
      }
   }

   static readonly ReentrancyGuard ReentrancyGuard = new();

   static void EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings() => ReentrancyGuard.ExecuteIfNotReEntering(() =>
   {
      State.Locked(state =>
      {
         var unHandledAssemblies = AppDomain.CurrentDomain.GetAssemblies().Except(state.CheckedAssemblies);

         foreach(var assembly in unHandledAssemblies)
         {
            if(state.CheckedAssemblies.Contains(assembly)) continue;

            try
            {
               ProcessAssembly(assembly, state);
            }
            finally
            {
               state.CheckedAssemblies.Add(assembly);
            }
         }

         if(ReentrancyGuard.GetAndClearReentryWasAttempted())
            EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings();
      });
   });

   static void ProcessAssembly(System.Reflection.Assembly assembly, MappingState state)
   {
      var assemblyMappings = AssemblyMappingReader.ReadMappings(assembly);
      foreach(var (type, typeId) in assemblyMappings)
         state.Map(type, typeId);

      var scannedTypes = TypeMapperAssemblyScanner.Scan(assembly);

      TypeId? ResolveExplicitTypeId(Type type) => state.TypeToTypeIdMap.GetValueOrDefault(type);

      foreach(var type in scannedTypes.TypesRequiringExplicitMapping.Concat(scannedTypes.ComposableTypes))
      {
         var mapperType = TypeMapperType.GetOrCreate(type, ResolveExplicitTypeId, state.TypeMapperTypeCache);
         if(mapperType.TypeId != null && !state.TypeToTypeIdMap.ContainsKey(type))
            state.Map(type, mapperType.TypeId);
      }

      var hasMissingExplicitMappings = scannedTypes.TypesRequiringExplicitMapping
         .Any(type => !state.TypeToTypeIdMap.ContainsKey(type));

      if(hasMissingExplicitMappings)
      {
         var tessage = MissingMappingReporter.BuildAssemblyMappingTessage(assembly, state.TypeToTypeIdMap);
         state.AssemblyMappingUpdateTessages[assembly] = tessage;
      }
   }

   static bool TryGetOrComputeTypeId(Type type, MappingState state, out TypeId typeId)
   {
      if(state.TypeToTypeIdMap.TryGetValue(type, out typeId!))
         return true;

      var mapperType = TypeMapperType.GetOrCreate(type, t => state.TypeToTypeIdMap.GetValueOrDefault(t), state.TypeMapperTypeCache);
      if(mapperType.TypeId != null)
      {
         typeId = mapperType.TypeId;
         state.Map(type, typeId);
         return true;
      }

      typeId = null!;
      return false;
   }

   class MappingState
   {
      internal readonly Dictionary<Type, TypeId> TypeToTypeIdMap = new();
      internal readonly Dictionary<TypeId, Type> TypeIdToTypeMap = new();
      internal readonly Dictionary<Type, TypeMapperType> TypeMapperTypeCache = new();
      internal readonly HashSet<System.Reflection.Assembly> CheckedAssemblies = [];
      internal readonly Dictionary<System.Reflection.Assembly, string> AssemblyMappingUpdateTessages = new();

      internal void Map(Type type, TypeId typeId)
      {
         if(TypeToTypeIdMap.TryGetValue(type, out var existingTypeId))
         {
            if(existingTypeId == typeId) return;
            throw new Exception($"Attempted to map Type:{type.FullName} to: {typeId}, but it is already mapped to: TypeId: {existingTypeId}");
         }

         if(TypeIdToTypeMap.TryGetValue(typeId, out var existingType))
         {
            if(existingType == type) return;
            throw new Exception($"Attempted to map TypeId:{typeId} to: {type.FullName}, but it is already mapped to Type: {existingType.FullName}");
         }

         AssertTypeValidForMapping(type);

         TypeIdToTypeMap.Add(typeId, type);
         TypeToTypeIdMap.Add(type, typeId);
      }
   }
}
