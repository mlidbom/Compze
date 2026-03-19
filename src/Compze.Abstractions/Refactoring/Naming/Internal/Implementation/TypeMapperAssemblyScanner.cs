using System.Reflection;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;
using static Compze.Abstractions.Refactoring.Naming.Internal.Implementation.TypeMapperType;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

class ScannedAssemblyTypes(
   IReadOnlyList<ExplicitlyMappedType> explicitlyMappedTypes,
   IReadOnlyList<ComputedTypeIdType> computedTypeIdTypes)
{
   internal IReadOnlyList<ExplicitlyMappedType> ExplicitlyMappedTypes { get; } = explicitlyMappedTypes;
   internal IReadOnlyList<ComputedTypeIdType> ComputedTypeIdTypes { get; } = computedTypeIdTypes;
}

static class TypeMapperAssemblyScanner
{
   internal static ScannedAssemblyTypes Scan(Assembly assembly)
   {
      var explicitRawTypes = new HashSet<Type>();
      var computedRawTypes = new HashSet<Type>();

      var definedTypes = assembly.GetTypes()
                                 .Where(type => !type.IsGenericParameter)
                                 .ToList();

      foreach(var type in definedTypes)
      {
         if(type.IsOpenGenericType()) continue;

         if(ShouldMapConcreteType(type))
         {
            explicitRawTypes.Add(type);
            computedRawTypes.Add(type.MakeArrayType());
         }
      }

      var nonGenericParameterTypes = definedTypes.Where(it => !it.IsOpenGenericType()).ToList();
      foreach(var definedType in nonGenericParameterTypes)
      {
         foreach(var property in definedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            CollectClosedGenericTypes(property.PropertyType, assembly, computedRawTypes, explicitRawTypes);

         foreach(var field in definedType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            CollectClosedGenericTypes(field.FieldType, assembly, computedRawTypes, explicitRawTypes);

         foreach(var method in definedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(method.ReturnType, assembly, computedRawTypes, explicitRawTypes);
            foreach(var parameter in method.GetParameters())
               CollectClosedGenericTypes(parameter.ParameterType, assembly, computedRawTypes, explicitRawTypes);
         }

         if(definedType.BaseType != null)
            CollectClosedGenericTypes(definedType.BaseType, assembly, computedRawTypes, explicitRawTypes);

         foreach(var interfaceType in definedType.GetInterfaces())
            CollectClosedGenericTypes(interfaceType, assembly, computedRawTypes, explicitRawTypes);
      }

      var classifiedExplicit = explicitRawTypes
                              .OrderBy(type => type.GetFullNameCompilable())
                              .Select(type => (ExplicitlyMappedType)TypeMapperType.FromType(type))
                              .ToList();

      var classifiedComputed = computedRawTypes
                              .OrderBy(type => type.GetFullNameCompilable())
                              .Select(type => (ComputedTypeIdType)TypeMapperType.FromType(type))
                              .ToList();

      return new ScannedAssemblyTypes(classifiedExplicit, classifiedComputed);
   }

   internal static bool IsAssemblyWeShouldExamine(Assembly assembly)
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

   static void CollectClosedGenericTypes(Type type, Assembly targetAssembly, HashSet<Type> computedRawTypes, HashSet<Type> explicitRawTypes)
   {
      if(type is { IsGenericType: true, IsGenericTypeDefinition: false })
      {
         var typeArguments = type.GetGenericArguments();
         var hasTypeArgumentFromTargetAssembly = typeArguments.Any(arg => !arg.IsGenericParameter && arg.Assembly == targetAssembly);

         if(hasTypeArgumentFromTargetAssembly && ShouldMapClosedGenericType(type))
         {
            computedRawTypes.Add(type);

            var genericDefinition = type.GetGenericTypeDefinition();
            if(genericDefinition.Assembly == targetAssembly)
               explicitRawTypes.Add(genericDefinition);

            foreach(var typeArg in typeArguments)
            {
               if(!typeArg.IsGenericParameter && typeArg.Assembly == targetAssembly
                                              && !typeArg.IsGenericType && !typeArg.IsArray)
                  explicitRawTypes.Add(typeArg);
            }
         }

         foreach(var typeArg in typeArguments)
         {
            if(!typeArg.IsGenericParameter)
               CollectClosedGenericTypes(typeArg, targetAssembly, computedRawTypes, explicitRawTypes);
         }
      }

      if(type.IsArray)
         CollectClosedGenericTypes(type.GetElementType()!, targetAssembly, computedRawTypes, explicitRawTypes);
   }

   static bool ShouldMapConcreteType(Type type)
   {
      if(type.IsGenericTypeDefinition || type.IsGenericParameter) return false;
      if(type.IsAbstract && !typeof(IRemotableTevent).IsAssignableFrom(type)) return false;

      return typeof(IRemotableTessage).IsAssignableFrom(type) ||
             type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>)) ||
             InheritsFromOpenGenericBase(type, typeof(EntityId<>));
   }

   static bool ShouldMapClosedGenericType(Type type)
   {
      if(type.IsGenericTypeDefinition || type.IsGenericParameter) return false;
      if(type.IsAbstract && !typeof(IRemotableTevent).IsAssignableFrom(type)) return false;

      return typeof(IRemotableTessage).IsAssignableFrom(type) ||
             type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
   }

   static bool InheritsFromOpenGenericBase(Type type, Type openGenericBase)
   {
      var current = type.BaseType;
      while(current != null)
      {
         if(current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBase) return true;
         current = current.BaseType;
      }

      return false;
   }
}
