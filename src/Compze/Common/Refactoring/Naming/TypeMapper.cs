using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Time;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.Threading.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Compze.Common.Refactoring.Naming;

static class TypeMapperRegistrar
{
   public static IComponentRegistrar TypeMapper(this IComponentRegistrar @this)
      => Naming.TypeMapper.RegisterWith(@this);
}

class TypeMapper : ITypeMapper
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<ITypeMapper, TypeMapper>()
                                  .Instance(Naming.TypeMapper.Instance));

   TypeMapper() {}
   internal static readonly ITypeMapper Instance = new TypeMapper();
   static readonly IThreadShared<MappingState> State = IThreadShared.WithDefaultTimeout<MappingState>();

   static TypeMapper()
   {
      EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings();
      AppDomain.CurrentDomain.AssemblyLoad += (_, _) => EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings();
   }

   public TypeId GetId(Type type)
   {
      return State.Read(state =>
      {
         if(state.TypeToTypeIdMap.TryGetValue(type, out var typeId))
         {
            return typeId;
         }

         // Check if we have a stored message for this assembly
         if(state.AssemblyMappingUpdateMessages.TryGetValue(type.Assembly, out var message))
         {
            throw new Exception($"Failed to find TypeId for type: {type.FullName}{Environment.NewLine}{message}");
         }

         throw BuildExceptionDescribingHowToAddMissingMappings([type]);
      });
   }

   public Type GetType(TypeId typeId)
   {
      return State.Read(state =>
      {
         if(state.TypeIdToTypeMap.TryGetValue(typeId, out var type))
         {
            return type;
         }

         throw new Exception($"Could not find type for {nameof(TypeId)}: {typeId}");
      });
   }

   public bool TryGetType(TypeId typeId, [NotNullWhen(true)] out Type? type)
   {
      type = State.Read(state => state.TypeIdToTypeMap.GetValueOrDefault(typeId));
      return type != null;
   }

   public IEnumerable<TypeId> GetIdForTypesAssignableTo(Type type)
   {
      return State.Read(state =>
      {
         var found = state
                    .TypeToTypeIdMap
                    .Keys
                    .Where(type.IsAssignableFrom)
                    .Select(matchingType => state.TypeToTypeIdMap[matchingType])
                    .ToArray();

         if(!found.Any())
         {
            // Check if we have a stored message for this assembly
            if(state.AssemblyMappingUpdateMessages.TryGetValue(type.Assembly, out var message))
            {
               throw new Exception($"Failed to find TypeIds for types assignable to: {type.FullName}{Environment.NewLine}{message}");
            }

            throw BuildExceptionDescribingHowToAddMissingMappings([type]);
         }

         return found;
      });
   }

   public unit AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings) => State.Update(state =>
   {
      var missing = typesThatRequireMappings.Where(type => !state.TypeToTypeIdMap.ContainsKey(type)).ToList();
      if(missing.Any()) throw BuildExceptionDescribingHowToAddMissingMappings(missing);
   });

   static void AssertTypeValidForMapping(Type type)
   {
      if(type.IsAbstract)
      {
         if(!typeof(IRemotableTevent).IsAssignableFrom(type))
         {
            throw new Exception($"Type: {type.FullName} is abstract and is not a {typeof(IRemotableTevent).FullName}. For other types you should only map concrete types.");
         }
      }
   }

   static readonly ReentrancyGuard _reentrancyGuard = new();

   static void EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings() => _reentrancyGuard.ExecuteIfNotReEntering(() =>
   {
      State.Update(state =>
      {
         var unHandledAssemblies = AppDomain.CurrentDomain.GetAssemblies().Except(state.CheckedAssemblies);

         foreach(var assembly in unHandledAssemblies)
         {
            {
               if(state.CheckedAssemblies.Contains(assembly))
               {
                  continue;
               }

               try
               {
                  CheckAssemblyForRequiredMappings(assembly, state);
               }
               finally
               {
                  state.CheckedAssemblies.Add(assembly);
               }
            }
         }

         if(_reentrancyGuard.GetAndClearReentryWasAttempted())
         {
            EnsureAllCurrentlyLoadedAssembliesHaveBeenCheckedForRequiredMappings();
         }
      });
   });

   static void CheckAssemblyForRequiredMappings(Assembly assembly, MappingState state)
   {
      var typesRequiringMapping = TypeMapperTypeDiscovery.GetTypesRequiringMapping(assembly);
      if(!typesRequiringMapping.Any()) return;

      var assemblyTypeMapperTypes = assembly.GetTypes()
                                            .Where(t => t.Name == TypeMapperSourceCodeGenerator.MappingClassName)
                                            .ToList();
      if(assemblyTypeMapperTypes.Count > 1)
         throw new Exception($"""
                              Found multiple type mappers for assembly:{assembly.FullName}
                              {assemblyTypeMapperTypes.Select(it => it.FullName!).Join(Environment.NewLine).Indent()}
                              """);

      var assemblyTypeMapperType = assemblyTypeMapperTypes.SingleOrDefault();

      if(assemblyTypeMapperType == null)
      {
         // Store the message for later use if type mapping fails
         var message = BuildMessageDescribingHowToAddMissingMappings(assembly);
         state.AssemblyMappingUpdateMessages[assembly] = message;
         return;
      }

      var instance = Activator.CreateInstance(assemblyTypeMapperType);
      var method = assemblyTypeMapperType.GetMethod("MapTypesForCurrentAssembly", BindingFlags.Public | BindingFlags.Instance);

      if(method != null && instance != null)
      {
         void MapAction(Guid guid, Type type) => state.Map(type, new TypeId(guid));
         method.Invoke(instance, [(Action<Guid, Type>)MapAction]);
      }

      var typesWithMissingMappings = typesRequiringMapping.Where(type => !state.TypeToTypeIdMap.ContainsKey(type)).ToList();
      if(typesWithMissingMappings.Any())
      {
         // Store the message for later use if type mapping fails
         var message = BuildMessageDescribingHowToAddMissingMappings(assembly);
         state.AssemblyMappingUpdateMessages[assembly] = message;
      }
   }

   static string BuildMessageDescribingHowToAddMissingMappings(Assembly assembly)
   {
      var assemblyName = assembly.GetName().Name;
      var rootNamespace = assemblyName;

      var allTypesRequiringMapping = TypeMapperTypeDiscovery.GetTypesRequiringMapping(assembly);

      // Get existing mappings for types in this assembly from the current TypeMapper state
      var existingMappings = State.Read(state => allTypesRequiringMapping
                                                .Where(type => state.TypeToTypeIdMap.ContainsKey(type))
                                                .ToDictionary(type => type, type => state.TypeToTypeIdMap[type]));

      var generatedCode = TypeMapperSourceCodeGenerator.GenerateAutoGeneratedClassCode(rootNamespace, allTypesRequiringMapping, existingMappings);

      // Try to automatically find the project file and create the mapping
      var createdFilePath = TypeMapperSourceCodeGenerator.TryFindProjectFileAndCreateMapping(assembly, allTypesRequiringMapping, existingMappings);

      var fixMessage = new StringBuilder();

      if(createdFilePath != null)
      {
         // File was auto-generated, but might be in wrong location (e.g., NCrunch temp folder)
         fixMessage.AppendLine(CultureInfo.InvariantCulture,
                               $"""

                                Type mappings were automatically generated for assembly: {assemblyName}
                                File location: {createdFilePath}

                                Please rebuild the project and try again.

                                IMPORTANT: If you are using a test runner like NCrunch that builds in a temporary location,
                                the generated file may not be in your source tree. In that case, please manually create
                                or update the file '{TypeMapperSourceCodeGenerator.MappingFileName}' in the root folder of the project '{assemblyName}'
                                with the following content:

                                """);
      } else
      {
         // Auto-generation failed, provide manual instructions
         fixMessage.AppendLine(CultureInfo.InvariantCulture,
                               $"""

                                In order to allow you to freely rename and move your types without breaking your persisted data you are required to map your types to Guid values that are used in place of your type names in the persisted data.
                                Some such required type mappings are missing for assembly: {assemblyName}

                                Please create a file named '{TypeMapperSourceCodeGenerator.MappingFileName}' in the root folder of the project '{assemblyName}' with the following content:

                                """);
      }

      fixMessage.AppendLine(generatedCode);

      return fixMessage.ToString();
   }

   static Exception BuildExceptionDescribingHowToAddMissingMappings(IReadOnlyList<Type> missingTypes)
   {
      var fixMessage = new StringBuilder();

      var firstType = missingTypes[0];
      var missingInTheSameAssembly = missingTypes.TakeWhile(it => it.Assembly == firstType.Assembly).ToList();

      fixMessage.AppendLine(CultureInfo.InvariantCulture,
                            $"""

                             In order to allow you to freely rename and move your types without breaking your persisted data you are required to map your types to Guid values that are used in place of your type names in the persisted data.
                             Some such required type mappings are missing. For convenience you can simply paste in the code below into the file {TypeMapperSourceCodeGenerator.MappingFileName} in the root of the project defining the type:
                             """);

      foreach(var missingType in missingInTheSameAssembly)
      {
         fixMessage.Append(CultureInfo.InvariantCulture, $"{Environment.NewLine}      map(new Guid(\"{Guid.NewGuid()}\"), typeof({missingType.GetFullNameCompilable()}));");
      }

      fixMessage.Append(Environment.NewLine).AppendLine();

      return new Exception(fixMessage.ToString());
   }

   class MappingState
   {
      public readonly Dictionary<Type, TypeId> TypeToTypeIdMap = new();
      public readonly Dictionary<TypeId, Type> TypeIdToTypeMap = new();
      public readonly HashSet<Assembly> CheckedAssemblies = [];
      public readonly Dictionary<Assembly, string> AssemblyMappingUpdateMessages = new();

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
            throw new Exception($"Attempted to map TypeId:{typeId.GuidValue} to: {type.FullName}, but it is already mapped to Type: {existingType.FullName}");
         }

         AssertTypeValidForMapping(type);

         TypeIdToTypeMap.Add(typeId, type);
         TypeToTypeIdMap.Add(type, typeId);
      }
   }
}
