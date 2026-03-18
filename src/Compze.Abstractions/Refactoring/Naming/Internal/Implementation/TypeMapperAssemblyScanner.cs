using System.Reflection;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

class ScannedAssemblyTypes(
   IReadOnlyList<Type> explicitlyMappedTypes,
   IReadOnlyList<Type> computedTypeIdTypes)
{
   static readonly ScannedAssemblyTypes _empty = new([], []);
   internal static ScannedAssemblyTypes Empty => _empty;

   internal IReadOnlyList<Type> ExplicitlyMappedTypes { get; } = explicitlyMappedTypes;
   internal IReadOnlyList<Type> ComputedTypeIdTypes { get; } = computedTypeIdTypes;
}

static class TypeMapperAssemblyScanner
{
   internal static ScannedAssemblyTypes Scan(Assembly assembly)
   {
      if(!IsAssemblyWeShouldExamine(assembly))
         return ScannedAssemblyTypes.Empty;

      var explicitTypes = new HashSet<Type>();
      var composableTypes = new HashSet<Type>();

      var definedTypes = assembly.GetTypes()
                                 .Where(type => !type.IsGenericParameter)
                                 .ToList();

      foreach(var type in definedTypes)
      {
         if(type.IsOpenGenericType()) continue;

         if(ShouldMapConcreteType(type))
         {
            explicitTypes.Add(type);
            composableTypes.Add(type.MakeArrayType());
         }
      }

      var nonGenericParameterTypes = definedTypes.Where(it => !it.IsOpenGenericType()).ToList();
      foreach(var definedType in nonGenericParameterTypes)
      {
         foreach(var property in definedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            CollectClosedGenericTypes(property.PropertyType, assembly, composableTypes, explicitTypes);

         foreach(var field in definedType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            CollectClosedGenericTypes(field.FieldType, assembly, composableTypes, explicitTypes);

         foreach(var method in definedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(method.ReturnType, assembly, composableTypes, explicitTypes);
            foreach(var parameter in method.GetParameters())
               CollectClosedGenericTypes(parameter.ParameterType, assembly, composableTypes, explicitTypes);
         }

         if(definedType.BaseType != null)
            CollectClosedGenericTypes(definedType.BaseType, assembly, composableTypes, explicitTypes);

         foreach(var interfaceType in definedType.GetInterfaces())
            CollectClosedGenericTypes(interfaceType, assembly, composableTypes, explicitTypes);
      }

      return new ScannedAssemblyTypes(
         explicitTypes.OrderBy(type => type.GetFullNameCompilable()).ToList(),
         composableTypes.OrderBy(type => type.GetFullNameCompilable()).ToList());
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
      if(type is { IsGenericType: true, IsGenericTypeDefinition: false })
      {
         var typeArguments = type.GetGenericArguments();
         var hasTypeArgumentFromTargetAssembly = typeArguments.Any(arg =>
                                                                      !arg.IsGenericParameter && arg.Assembly == targetAssembly);

         if(hasTypeArgumentFromTargetAssembly && ShouldMapClosedGenericType(type))
         {
            composableTypes.Add(type);

            var genericDefinition = type.GetGenericTypeDefinition();
            if(genericDefinition.Assembly == targetAssembly)
               explicitTypes.Add(genericDefinition);

            foreach(var typeArg in typeArguments)
            {
               if(!typeArg.IsGenericParameter && typeArg.Assembly == targetAssembly
                  && !typeArg.IsGenericType && !typeArg.IsArray)
                  explicitTypes.Add(typeArg);
            }
         }

         foreach(var typeArg in typeArguments)
         {
            if(!typeArg.IsGenericParameter)
               CollectClosedGenericTypes(typeArg, targetAssembly, composableTypes, explicitTypes);
         }
      }

      if(type.IsArray)
         CollectClosedGenericTypes(type.GetElementType()!, targetAssembly, composableTypes, explicitTypes);
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
