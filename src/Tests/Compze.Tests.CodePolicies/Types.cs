using System;
using System.Linq;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

namespace Compze.Tests.CodePolicies;

public static class Types
{
   public static class InNamespace
   {
      public static class Abstractions
      {
         public static class Internal
         {
            [XFact] public static void ShouldBeInternal()
            {
               CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

               var allCompzeTypes = AppDomain.CurrentDomain.AllCompzeTypes();

               var abstractionsInternalTypes = allCompzeTypes
                                              .Where(type => type.Namespace?.Contains(".Abstractions.Internal", StringComparison.Ordinal) == true)
                                              .ToList();

               var publicTypes = abstractionsInternalTypes
                                .Where(type => type.IsPublic || type.IsNestedPublic)
                                .ToList();

               publicTypes.Must().BeEmpty($"""
                                           All types in *.Abstractions.Internal namespaces should be internal, but we found public types:
                                           {string.Join(Environment.NewLine, publicTypes.Select(t => t.FullName))}
                                           """);
            }
         }
      }
   }
}
