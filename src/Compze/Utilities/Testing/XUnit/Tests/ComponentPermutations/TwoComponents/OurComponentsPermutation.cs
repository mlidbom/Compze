using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents;

static class OurComponentsPermutation
{
   public static Serializer Serializer(this ComponentsPermutation? permutation) =>
      (Serializer)permutation!.Components[0];

   public static SqlLayer SqlLayer(this ComponentsPermutation? permutation) =>
      (SqlLayer)permutation!.Components[1];
}
