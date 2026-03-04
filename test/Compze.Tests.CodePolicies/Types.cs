using Compze.Utilities.SystemCE;
using Compze.Must;
using Compze.xUnit.BDD;

namespace Compze.Tests.CodePolicies;

#pragma warning disable CA1724 //The type name Types conflicts in whole or in part with the namespace
public static class Types
{
   public static class InNamespace
   {
      public static class Abstractions
      {
         public static class Internal
         {
            [XF(Skip = "TODO")] public static void ShouldBeInternal()
            {
               CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

               var allCompzeTypes = AppDomain.CurrentDomain.AllCompzeTypes();

               var abstractionsInternalTypes = allCompzeTypes
                                              .Where(type => type.Namespace?.ContainsCE(".Abstractions.Internal") == true)
                                              .ToList();

               var publicTypes = abstractionsInternalTypes
                                .Where(type => type.IsPublic || type.IsNestedPublic)
                                .ToList();

               publicTypes.Must().BeEmpty();
            }
         }
      }
   }
}
#pragma warning restore CA1724 //The type name Types conflicts in whole or in part with the namespace
