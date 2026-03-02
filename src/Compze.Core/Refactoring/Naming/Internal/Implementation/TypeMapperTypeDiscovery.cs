using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compze.Core.Public;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Core.Refactoring.Naming.Internal.Implementation;

static class TypeMapperTypeDiscovery
{
   public static ISet<Type> GetTypesRequiringMapping(Assembly assembly)
   {
      if(!IsAssemblyWeShouldExamine(assembly)) return new HashSet<Type>();
      var types = new HashSet<Type>();

      // Get all types defined in the assembly
      var definedTypes = assembly.GetTypes()
                                 .Where(type => !type.IsGenericParameter)
                                 .Where(it => !it.IsOpenGenericType())
                                 .ToList();

      // Add non-generic types and generic type definitions that need mapping
      foreach(var type in definedTypes)
      {
         if(ShouldMapType(type))
         {
            types.Add(type);
            // Also add the array type for each mapped type
            types.Add(type.MakeArrayType());
         }
      }

      // Find all closed generic type instantiations used in this assembly
      foreach(var definedType in definedTypes)
      {
         // Check properties
         foreach(var property in definedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(property.PropertyType, assembly, types);
         }

         // Check fields
         foreach(var field in definedType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(field.FieldType, assembly, types);
         }

         // Check methods (parameters and return types)
         foreach(var method in definedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(method.ReturnType, assembly, types);
            foreach(var parameter in method.GetParameters())
            {
               CollectClosedGenericTypes(parameter.ParameterType, assembly, types);
            }
         }

         // Check base type
         if(definedType.BaseType != null)
         {
            CollectClosedGenericTypes(definedType.BaseType, assembly, types);
         }

         // Check interfaces
         foreach(var interfaceType in definedType.GetInterfaces())
         {
            CollectClosedGenericTypes(interfaceType, assembly, types);
         }
      }

      return types.OrderBy(type => type.GetFullNameCompilable()).ToHashSet();
   }

   static bool IsAssemblyWeShouldExamine(Assembly assembly)
   {
      if(assembly.IsDynamic || assembly.FullName == null)
         return false;

      const string compzeAssemblyNamesStart = "Compze.";

      if(assembly.FullName.StartsWithCE(compzeAssemblyNamesStart))
         return true;

      if(assembly.GetReferencedAssemblies().Any(name => name.Name != null && name.Name.StartsWithCE(compzeAssemblyNamesStart)))
         return true;

      return false;
   }

   static void CollectClosedGenericTypes(Type type, Assembly targetAssembly, HashSet<Type> collectedTypes)
   {
      // If this is a closed generic type (generic but not a generic type definition)
      if(type.IsGenericType && !type.IsGenericTypeDefinition)
      {
         // Check if any of the type arguments are defined in the target assembly
         var typeArguments = type.GetGenericArguments();
         var hasTypeArgumentFromTargetAssembly = typeArguments.Any(arg =>
                                                                      !arg.IsGenericParameter && arg.Assembly == targetAssembly);

         if(hasTypeArgumentFromTargetAssembly && ShouldMapType(type))
         {
            collectedTypes.Add(type);
         }

         // Recursively check type arguments for nested generics
         foreach(var typeArg in typeArguments)
         {
            if(!typeArg.IsGenericParameter)
            {
               CollectClosedGenericTypes(typeArg, targetAssembly, collectedTypes);
            }
         }
      }

      // Check if the type itself is generic and recurse
      if(type.IsArray)
      {
         CollectClosedGenericTypes(type.GetElementType()!, targetAssembly, collectedTypes);
      }
   }

   static bool ShouldMapType(Type type)
   {
      // Don't map open generic type definitions or generic parameters
      if(type.IsGenericTypeDefinition || type.IsGenericParameter)
      {
         return false;
      }

      // Only map non-abstract types, or abstract types that are IRemotableTevent
      if(type.IsAbstract && !typeof(IRemotableTevent).IsAssignableFrom(type))
      {
         return false;
      }

      // Map if it's an ITessage or implements IHasPersistentIdentity<>
      return typeof(IRemotableTessage).IsAssignableFrom(type) ||
             type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
   }
}
