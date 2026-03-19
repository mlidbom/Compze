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

   public TypeId GetId(Type type) => State.Locked(state =>
   {
      if(TryResolveTypeId(type, state, out var typeId))
         return typeId;

      if(state.AssemblyMappingUpdateMessages.TryGetValue(type.Assembly, out var assemblyMappingMessage))
         throw new Exception($"Failed to find TypeId for type: {type.FullName}{Environment.NewLine}{assemblyMappingMessage}");

      throw MissingMappingReporter.BuildMissingTypesException([type]);
   });

   public Type GetType(TypeId teventTypeId) => State.Locked(state =>
   {
      if(state.TypeIdToTypeMap.TryGetValue(teventTypeId, out var type))
         return type;

      throw new Exception($"Could not find type for {nameof(TypeId)}: {teventTypeId}");
   });

   public bool TryGetType(TypeId typeId, [NotNullWhen(true)] out Type? type)
   {
      type = State.Locked(state => state.TypeIdToTypeMap.GetValueOrDefault(typeId));
      return type != null;
   }

   public IEnumerable<TypeId> GetIdForTypesAssignableTo(Type type) => State.Locked(state =>
   {
      EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings();

      TryResolveTypeId(type, state, out _);

      var found = state
                 .TypeToTypeIdMap
                 .Keys
                 .Where(type.IsAssignableFrom)
                 .Select(matchingType => state.TypeToTypeIdMap[matchingType])
                 .ToArray();

      if(!found.Any())
      {
         if(state.AssemblyMappingUpdateMessages.TryGetValue(type.Assembly, out var message))
            throw new Exception($"Failed to find TypeIds for types assignable to: {type.FullName}{Environment.NewLine}{message}");

         throw MissingMappingReporter.BuildMissingTypesException([type]);
      }

      return found;
   });

   public Unit AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings) => State.Locked(state =>
   {
      var missing = typesThatRequireMappings.Where(type => !TryResolveTypeId(type, state, out _)).ToList();
      if(missing.Any()) throw MissingMappingReporter.BuildMissingTypesException(missing);
   });

   static readonly ReentrancyGuard ReentrancyGuard = new();

   static void EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings() => ReentrancyGuard.ExecuteIfNotReEntering(() =>
   {
      State.Locked(state =>
      {
         foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies().Except(state.CheckedAssemblies))
         {
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
      if(TypeMapperAssemblyScanner.IsAssemblyWeShouldExamine(assembly))
      {
         state.AddMappings(AssemblyMappingReader.ReadMappings(assembly));

         var scannedTypes = TypeMapperAssemblyScanner.Scan(assembly);

         foreach(var classifiedType in scannedTypes.ExplicitlyMappedTypes.Concat<TypeMapperType>(scannedTypes.ComputedTypeIdTypes))
         {
            var typeId = ResolveTypeId(classifiedType, state);
            if(typeId != null && !state.TypeToTypeIdMap.ContainsKey(classifiedType.Type))
               state.AddMapping(classifiedType.Type, typeId);
         }

         var hasMissingExplicitMappings = scannedTypes.ExplicitlyMappedTypes
                                                      .Any(explicitType => !state.TypeToTypeIdMap.ContainsKey(explicitType.Type));

         if(hasMissingExplicitMappings)
         {
            var message = MissingMappingReporter.BuildAssemblyMappingMessage(assembly, state.TypeToTypeIdMap);
            state.AssemblyMappingUpdateMessages[assembly] = message;
         }
      }
   }

   /// <summary>Resolves a TypeId for a classified type by walking the structural hierarchy.
   /// <see cref="TypeMapperType.ExplicitlyMappedType"/>: looked up in the mapping dictionary.
   /// <see cref="TypeMapperType.ClosedGenericType"/>: computed from definition + argument TypeIds.
   /// <see cref="TypeMapperType.ArrayType"/>: computed from element TypeId.</summary>
   static TypeId? ResolveTypeId(TypeMapperType classifiedType, MappingState state)
   {
      switch(classifiedType)
      {
         case TypeMapperType.ExplicitlyMappedType:
            return state.TypeToTypeIdMap.GetValueOrDefault(classifiedType.Type);

         case TypeMapperType.ClosedGenericType closedGeneric:
         {
            var definitionId = ResolveTypeId(closedGeneric.OpenGenericType, state);
            if(definitionId == null)
               return null;

            var argumentIds = new TypeId[closedGeneric.TypeArguments.Count];
            for(var i = 0; i < closedGeneric.TypeArguments.Count; i++)
            {
               var argId = ResolveTypeId(closedGeneric.TypeArguments[i], state);
               if(argId == null)
                  return null;
               argumentIds[i] = argId;
            }

            return DeterministicTypeIdGenerator.Generate(definitionId, argumentIds);
         }

         case TypeMapperType.ArrayType arrayType:
         {
            var elementId = ResolveTypeId(arrayType.ElementType, state);
            if(elementId == null) return null;
            return DeterministicTypeIdGenerator.Generate(DeterministicTypeIdGenerator.ArrayMarkerTypeId, elementId);
         }

         default:
            return null;
      }
   }

   /// <summary>Tries to resolve a TypeId for a raw <see cref="Type"/> at runtime (e.g. types not seen during assembly scanning).
   /// Classifies the type structurally, then walks the hierarchy to resolve.</summary>
   static bool TryResolveTypeId(Type type, MappingState state, out TypeId typeId)
   {
      if(state.TypeToTypeIdMap.TryGetValue(type, out typeId!))
         return true;

      var classifiedType = TypeMapperType.FromType(type);
      var resolved = ResolveTypeId(classifiedType, state);
      if(resolved != null)
      {
         typeId = resolved;
         state.AddMapping(type, typeId);
         return true;
      }

      typeId = null!;
      return false;
   }

   class MappingState
   {
      internal readonly Dictionary<Type, TypeId> TypeToTypeIdMap = new();
      internal readonly Dictionary<TypeId, Type> TypeIdToTypeMap = new();
      internal readonly HashSet<System.Reflection.Assembly> CheckedAssemblies = [];
      internal readonly Dictionary<System.Reflection.Assembly, string> AssemblyMappingUpdateMessages = new();

      internal void AddMappings(IReadOnlyDictionary<Type, TypeId> ids) => ids.ForEach(keyValuePair => AddMapping(keyValuePair.Key, keyValuePair.Value));

      internal void AddMapping(Type type, TypeId typeId)
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

   static void AssertTypeValidForMapping(Type type)
   {
      if(type.IsGenericTypeDefinition) return;

      if(type.IsAbstract)
      {
         if(!typeof(IRemotableTevent).IsAssignableFrom(type))
            throw new Exception($"Type: {type.FullName} is abstract and is not a {typeof(IRemotableTevent).FullName}. For other types you should only map concrete types.");
      }
   }
}
