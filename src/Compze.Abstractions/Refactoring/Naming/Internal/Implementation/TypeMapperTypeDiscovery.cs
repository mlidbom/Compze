using System.Reflection;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

static class TypeMapperTypeDiscovery
{
   internal class DiscoveredTypes
   {
      internal readonly ISet<Type> RequiringExplicitMapping;
      internal readonly ISet<Type> Composable;

      internal DiscoveredTypes(ISet<Type> requiringExplicitMapping, ISet<Type> composable)
      {
         RequiringExplicitMapping = requiringExplicitMapping;
         Composable = composable;
      }
   }

   internal static DiscoveredTypes DiscoverTypes(Assembly assembly)
   {
      if(!IsAssemblyWeShouldExamine(assembly))
         return new DiscoveredTypes(new HashSet<Type>(), new HashSet<Type>());

      var explicitTypes = new HashSet<Type>();
      var composableTypes = new HashSet<Type>();

      // Get all types defined in the assembly
      var definedTypes = assembly.GetTypes()
                                 .Where(type => !type.IsGenericParameter)
                                 .ToList();

      // Add non-generic concrete types that need mapping
      foreach(var type in definedTypes)
      {
         if(type.IsOpenGenericType()) continue;

         if(ShouldMapConcreteType(type))
         {
            explicitTypes.Add(type);
            composableTypes.Add(type.MakeArrayType());
         }
      }

      // Find all closed generic type instantiations used in this assembly
      var nonGenericParameterTypes = definedTypes.Where(it => !it.IsOpenGenericType()).ToList();
      foreach(var definedType in nonGenericParameterTypes)
      {
         // Check properties
         foreach(var property in definedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(property.PropertyType, assembly, composableTypes, explicitTypes);
         }

         // Check fields
         foreach(var field in definedType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(field.FieldType, assembly, composableTypes, explicitTypes);
         }

         // Check methods (parameters and return types)
         foreach(var method in definedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(method.ReturnType, assembly, composableTypes, explicitTypes);
            foreach(var parameter in method.GetParameters())
            {
               CollectClosedGenericTypes(parameter.ParameterType, assembly, composableTypes, explicitTypes);
            }
         }

         // Check base type
         if(definedType.BaseType != null)
         {
            CollectClosedGenericTypes(definedType.BaseType, assembly, composableTypes, explicitTypes);
         }

         // Check interfaces
         foreach(var interfaceType in definedType.GetInterfaces())
         {
            CollectClosedGenericTypes(interfaceType, assembly, composableTypes, explicitTypes);
         }
      }

      return new DiscoveredTypes(
         explicitTypes.OrderBy(type => type.GetFullNameCompilable()).ToHashSet(),
         composableTypes.OrderBy(type => type.GetFullNameCompilable()).ToHashSet());
   }

   static bool IsAssemblyWeShouldExamine(Assembly assembly)
   {
      if(assembly.IsDynamic || assembly.FullName == null)
         return false;

      const string compzeAssemblyNamesStart = "Compze.";

      if(assembly.FullName.StartsWithOrdinal(compzeAssemblyNamesStart))
         return true;

      if(assembly.GetReferencedAssemblies().Any(name => name.Name != null && name.Name.StartsWithOrdinal(compzeAssemblyNamesStart)))
         return true;

      return false;
   }

   static void CollectClosedGenericTypes(Type type, Assembly targetAssembly, HashSet<Type> composableTypes, HashSet<Type> explicitTypes)
   {
      // If this is a closed generic type (generic but not a generic type definition)
      if(type is { IsGenericType: true, IsGenericTypeDefinition: false })
      {
         // Check if any of the type arguments are defined in the target assembly
         var typeArguments = type.GetGenericArguments();
         var hasTypeArgumentFromTargetAssembly = typeArguments.Any(arg =>
                                                                      !arg.IsGenericParameter && arg.Assembly == targetAssembly);

         if(hasTypeArgumentFromTargetAssembly && ShouldMapClosedGenericType(type))
         {
            composableTypes.Add(type);

            // The open generic definition needs an explicit mapping to survive renames
            var genericDefinition = type.GetGenericTypeDefinition();
            if(genericDefinition.Assembly == targetAssembly)
            {
               explicitTypes.Add(genericDefinition);
            }

            // Type arguments from the target assembly need explicit mappings to survive renames
            foreach(var typeArg in typeArguments)
            {
               if(!typeArg.IsGenericParameter && typeArg.Assembly == targetAssembly
                  && !typeArg.IsGenericType && !typeArg.IsArray)
               {
                  explicitTypes.Add(typeArg);
               }
            }
         }

         // Recursively check type arguments for nested generics
         foreach(var typeArg in typeArguments)
         {
            if(!typeArg.IsGenericParameter)
            {
               CollectClosedGenericTypes(typeArg, targetAssembly, composableTypes, explicitTypes);
            }
         }
      }

      // Check if the type itself is generic and recurse
      if(type.IsArray)
      {
         CollectClosedGenericTypes(type.GetElementType()!, targetAssembly, composableTypes, explicitTypes);
      }
   }

   static bool ShouldMapConcreteType(Type type)
   {
      if(type.IsGenericTypeDefinition || type.IsGenericParameter)
         return false;

      if(type.IsAbstract && !typeof(IRemotableTevent).IsAssignableFrom(type))
         return false;

      return typeof(IRemotableTessage).IsAssignableFrom(type) ||
             type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>)) ||
             InheritsFromOpenGenericBase(type, typeof(EntityId<>));
   }

   static bool ShouldMapClosedGenericType(Type type)
   {
      if(type.IsGenericTypeDefinition || type.IsGenericParameter)
         return false;

      if(type.IsAbstract && !typeof(IRemotableTevent).IsAssignableFrom(type))
         return false;

      return typeof(IRemotableTessage).IsAssignableFrom(type) ||
             type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
   }

   static bool InheritsFromOpenGenericBase(Type type, Type openGenericBase)
   {
      var current = type.BaseType;
      while(current != null)
      {
         if(current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBase)
            return true;
         current = current.BaseType;
      }
      return false;
   }

}
