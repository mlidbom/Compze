using Compze.Internals.SystemCE;
using Compze.Must;

using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

public static class AppDomainExtensionsTests
{
   public static class AllCompzeTypes
   {
      [XF(Skip = "We are in the middle of a major refactoring and this frequently breaks the tests")]
      public static void ReturnsTypesFromEveryCompzeAssembly()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = AppDomain.CurrentDomain.AllCompzeTypes();
         var compzeAssemblies = AppDomain.CurrentDomain
                                      .GetAssemblies()
                                      .Where(assembly => assembly.GetName().Name?.StartsWithOrdinal("Compze.") == true)
                                      .ToList();

         compzeAssemblies.Must().NotBeEmpty();

         var assembliesWithTypes = allTypes
                                  .GroupBy(type => type.Assembly)
                                  .Select(group => group.Key)
                                  .ToList();

         var assembliesWithoutTypes = compzeAssemblies
                                     .Where(assembly => !assembliesWithTypes.Contains(assembly))
                                     .ToList();

         assembliesWithoutTypes.Must().BeEmpty();
      }

      [XF]
      public static void ReturnsBothPublicAndInternalTypes()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = AppDomain.CurrentDomain.AllCompzeTypes();

         var publicTypes = allTypes.Where(type => type.IsPublic || type.IsNestedPublic).ToList();
         var internalTypes = allTypes.Where(type => type is { IsPublic: false, IsNestedPublic: false, IsNestedPrivate: false, IsNestedFamily: false, IsNestedFamORAssem: false, IsNestedFamANDAssem: false }).ToList();

         publicTypes.Must().NotBeEmpty();
         internalTypes.Must().NotBeEmpty();
      }

      [XF]
      public static void ReturnsClasses()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = AppDomain.CurrentDomain.AllCompzeTypes();
         var classes = allTypes.Where(type => type.IsClass).ToList();

         classes.Must().NotBeEmpty();
      }

      [XF]
      public static void ReturnsStructs()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = AppDomain.CurrentDomain.AllCompzeTypes();
         var structs = allTypes.Where(type => type is { IsValueType: true, IsEnum: false }).ToList();

         structs.Must().NotBeEmpty();
      }

      [XF]
      public static void ReturnsInterfaces()
      {
         CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

         var allTypes = AppDomain.CurrentDomain.AllCompzeTypes();
         var interfaces = allTypes.Where(type => type.IsInterface).ToList();

         interfaces.Must().NotBeEmpty();
      }
   }
}
