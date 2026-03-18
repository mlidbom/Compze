using System.Reflection;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;
using static Compze.Abstractions.Refactoring.Naming.Internal.Implementation.TypeMapperType;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

class ScannedAssemblyTypes(
   IReadOnlyList<LeafType> leafTypes,
   IReadOnlyList<OpenGenericDefinition> openGenericDefinitions,
   IReadOnlyList<ClosedGenericType> closedGenericTypes,
   IReadOnlyList<ArrayType> arrayTypes)
{
   static readonly ScannedAssemblyTypes _empty = new([], [], [], []);
   internal static ScannedAssemblyTypes Empty => _empty;

   internal IReadOnlyList<LeafType> LeafTypes { get; } = leafTypes;
   internal IReadOnlyList<OpenGenericDefinition> OpenGenericDefinitions { get; } = openGenericDefinitions;
   internal IReadOnlyList<ClosedGenericType> ClosedGenericTypes { get; } = closedGenericTypes;
   internal IReadOnlyList<ArrayType> ArrayTypes { get; } = arrayTypes;
}

static class TypeMapperAssemblyScanner
{
   internal static ScannedAssemblyTypes Scan(Assembly assembly)
   {
      if(!IsAssemblyWeShouldExamine(assembly))
         return ScannedAssemblyTypes.Empty;

      var leafTypes = new HashSet<Type>();
      var openGenericDefinitions = new HashSet<Type>();
      var closedGenericTypes = new HashSet<Type>();
      var arrayElementTypes = new HashSet<Type>();

      var definedTypes = assembly.GetTypes()
                                 .Where(type => !type.IsGenericParameter)
                                 .ToList();

      foreach(var type in definedTypes)
      {
         if(type.IsOpenGenericType()) continue;

         if(ShouldMapConcreteType(type))
         {
            leafTypes.Add(type);
            arrayElementTypes.Add(type);
         }
      }

      var nonGenericParameterTypes = definedTypes.Where(it => !it.IsOpenGenericType()).ToList();
      foreach(var definedType in nonGenericParameterTypes)
      {
         foreach(var property in definedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            CollectClosedGenericTypes(property.PropertyType, assembly, closedGenericTypes, openGenericDefinitions, leafTypes);

         foreach(var field in definedType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            CollectClosedGenericTypes(field.FieldType, assembly, closedGenericTypes, openGenericDefinitions, leafTypes);

         foreach(var method in definedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
         {
            CollectClosedGenericTypes(method.ReturnType, assembly, closedGenericTypes, openGenericDefinitions, leafTypes);
            foreach(var parameter in method.GetParameters())
               CollectClosedGenericTypes(parameter.ParameterType, assembly, closedGenericTypes, openGenericDefinitions, leafTypes);
         }

         if(definedType.BaseType != null)
            CollectClosedGenericTypes(definedType.BaseType, assembly, closedGenericTypes, openGenericDefinitions, leafTypes);

         foreach(var interfaceType in definedType.GetInterfaces())
            CollectClosedGenericTypes(interfaceType, assembly, closedGenericTypes, openGenericDefinitions, leafTypes);
      }

      return new ScannedAssemblyTypes(
         leafTypes.OrderBy(t => t.GetFullNameCompilable()).Select(t => new LeafType(t)).ToList(),
         openGenericDefinitions.OrderBy(t => t.GetFullNameCompilable()).Select(t => new OpenGenericDefinition(t)).ToList(),
         closedGenericTypes.OrderBy(t => t.GetFullNameCompilable()).Select(t => new ClosedGenericType(t)).ToList(),
         arrayElementTypes.OrderBy(t => t.GetFullNameCompilable()).Select(t => new ArrayType(t.MakeArrayType())).ToList());
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

   static void CollectClosedGenericTypes(Type type, Assembly targetAssembly, HashSet<Type> closedGenericTypes, HashSet<Type> openGenericDefinitions, HashSet<Type> leafTypes)
   {
      if(type is { IsGenericType: true, IsGenericTypeDefinition: false })
      {
         var typeArguments = type.GetGenericArguments();
         var hasTypeArgumentFromTargetAssembly = typeArguments.Any(arg =>
                                                                      !arg.IsGenericParameter && arg.Assembly == targetAssembly);

         if(hasTypeArgumentFromTargetAssembly && ShouldMapClosedGenericType(type))
         {
            closedGenericTypes.Add(type);

            var genericDefinition = type.GetGenericTypeDefinition();
            if(genericDefinition.Assembly == targetAssembly)
               openGenericDefinitions.Add(genericDefinition);

            foreach(var typeArg in typeArguments)
            {
               if(!typeArg.IsGenericParameter && typeArg.Assembly == targetAssembly
                  && !typeArg.IsGenericType && !typeArg.IsArray)
                  leafTypes.Add(typeArg);
            }
         }

         foreach(var typeArg in typeArguments)
         {
            if(!typeArg.IsGenericParameter)
               CollectClosedGenericTypes(typeArg, targetAssembly, closedGenericTypes, openGenericDefinitions, leafTypes);
         }
      }

      if(type.IsArray)
         CollectClosedGenericTypes(type.GetElementType()!, targetAssembly, closedGenericTypes, openGenericDefinitions, leafTypes);
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
