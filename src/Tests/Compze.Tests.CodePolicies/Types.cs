using System;
using System.Linq;
using System.Reflection;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;

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

               publicTypes.Should().BeEmpty($"""
                                             All types in *.Abstractions.Internal namespaces should be internal, but found:
                                              {string.Join(Environment.NewLine, publicTypes.Select(t => t.FullName).ToList())}
                                             """);
            }
         }
      }
   }
}
