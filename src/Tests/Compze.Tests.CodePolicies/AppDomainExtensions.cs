using System;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Tests.CodePolicies;

static class AppDomainExtensions
{
   public static IReadOnlyList<Type> AllCompzeTypes(this AppDomain appDomain)
   {
      return appDomain.GetAssemblies()
                      .Where(assembly => assembly.GetName().Name?.StartsWith("Compze.", StringComparison.Ordinal) == true)
                      .SelectMany(assembly => assembly.GetTypes())
                      .ToList();
   }
}
