using Compze.Utilities.Testing.XUnit.ComponentsCombinations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;

static class OurComponentsCombination
{
   public static Serializer Serializer(this ComponentsCombination? combination) =>
      (Serializer)combination!.Components[0];

   public static SqlLayer SqlLayer(this ComponentsCombination? combination) =>
      (SqlLayer)combination!.Components[1];
}
