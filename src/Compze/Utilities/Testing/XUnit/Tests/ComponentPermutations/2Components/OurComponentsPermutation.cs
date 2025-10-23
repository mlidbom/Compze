using Compze.Utilities.Testing.XUnit.ComponentsPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components;

static class OurComponentsPermutation
{
   public static Serializer Serializer(this ComponentsPermutation? permutation) =>
      (Serializer)permutation!.Components[0];

   public static SqlLayer SqlLayer(this ComponentsPermutation? permutation) =>
      (SqlLayer)permutation!.Components[1];
}
