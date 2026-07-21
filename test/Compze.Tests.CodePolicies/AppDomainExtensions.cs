using System.Reflection;
using Compze.Internals.SystemCE;

namespace Compze.Tests.CodePolicies;

static class AppDomainExtensions
{
   extension(AppDomain @this)
   {
      public IReadOnlyList<Type> AllCompzeTypes() =>
         [
            .. @this.GetAssemblies()
                    .Where(assembly => assembly.GetName().Name?.StartsWithOrdinal("Compze.") == true)
                    .SelectMany(assembly => assembly.GetTypes())
         ];

      ///<summary>Every type in the loaded Compze library assemblies — the shipped packages the code policies govern.<br/>
      /// Test assemblies (<c>*.Tests*</c>, <c>*.Specifications</c>, <c>*.InternalSpecifications</c>) are not policed.</summary>
      public IReadOnlyList<Type> AllCompzeLibraryTypes() =>
         [
            .. @this.GetAssemblies()
                    .Where(IsCompzeLibraryAssembly)
                    .SelectMany(assembly => assembly.GetTypes())
         ];
   }

   static bool IsCompzeLibraryAssembly(Assembly assembly)
   {
      var name = assembly.GetName().Name!;
      return name.StartsWithOrdinal("Compze.") && !name.ContainsOrdinal(".Tests") && !name.EndsWithOrdinal("Specifications");
   }
}
