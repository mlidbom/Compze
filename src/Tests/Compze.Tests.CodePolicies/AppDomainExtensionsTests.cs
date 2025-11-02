using System.Linq;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.CodePolicies;

public static class AppDomainExtensionsTests
{
   public static class AllCompzeTypes
   {
      [XF(Skip = "We are in the middle of a major refactoring and this frequently breaks the tests")]
      public static void ReturnsTypesFromEveryCompzeAssembly()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = System.AppDomain.CurrentDomain.AllCompzeTypes();
         var compzeAssemblies = System.AppDomain.CurrentDomain
                                      .GetAssemblies()
                                      .Where(assembly => assembly.GetName().Name?.StartsWithOrdinal("Compze.") == true)
                                      .ToList();

         compzeAssemblies.Must().NotBeEmpty("there should be Compze assemblies loaded");

         var assembliesWithTypes = allTypes
                                  .GroupBy(type => type.Assembly)
                                  .Select(group => group.Key)
                                  .ToList();

         var assembliesWithoutTypes = compzeAssemblies
                                     .Where(assembly => !assembliesWithTypes.Contains(assembly))
                                     .ToList();

         assembliesWithoutTypes.Must().BeEmpty(
            $"""
             every Compze assembly should have at least one type, but these assemblies have no types: 
             {assembliesWithoutTypes.Select(a => a.GetName().FullName).JoinLines().Indent()}
             """);
      }

      [XF]
      public static void ReturnsBothPublicAndInternalTypes()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = System.AppDomain.CurrentDomain.AllCompzeTypes();

         var publicTypes = allTypes.Where(type => type.IsPublic || type.IsNestedPublic).ToList();
         var internalTypes = allTypes.Where(type => !type.IsPublic && !type.IsNestedPublic && !type.IsNestedPrivate && !type.IsNestedFamily && !type.IsNestedFamORAssem && !type.IsNestedFamANDAssem).ToList();

         publicTypes.Must().NotBeEmpty("there should be public types in Compze assemblies");
         internalTypes.Must().NotBeEmpty("there should be internal types in Compze assemblies");
      }

      [XF]
      public static void ReturnsClasses()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = System.AppDomain.CurrentDomain.AllCompzeTypes();
         var classes = allTypes.Where(type => type.IsClass).ToList();

         classes.Must().NotBeEmpty("there should be classes in Compze assemblies");
      }

      [XF]
      public static void ReturnsStructs()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = System.AppDomain.CurrentDomain.AllCompzeTypes();
         var structs = allTypes.Where(type => type.IsValueType && !type.IsEnum).ToList();

         structs.Must().NotBeEmpty("there should be structs in Compze assemblies");
      }

      [XF]
      public static void ReturnsInterfaces()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = System.AppDomain.CurrentDomain.AllCompzeTypes();
         var interfaces = allTypes.Where(type => type.IsInterface).ToList();

         interfaces.Must().NotBeEmpty("there should be interfaces in Compze assemblies");
      }
   }
}
