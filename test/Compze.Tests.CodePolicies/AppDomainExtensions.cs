using Compze.Internals.SystemCE;

namespace Compze.Tests.CodePolicies;

static class AppDomainExtensions
{
   public static IReadOnlyList<Type> AllCompzeTypes(this AppDomain appDomain)
   {
      return appDomain.GetAssemblies()
                      .Where(assembly => assembly.GetName().Name?.StartsWithOrdinal("Compze.") == true)
                      .SelectMany(assembly => assembly.GetTypes())
                      .ToList();
   }
}
