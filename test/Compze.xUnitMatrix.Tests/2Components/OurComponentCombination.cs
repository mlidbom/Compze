namespace Compze.xUnitMatrix.Tests._2Components;

static class OurComponentCombination
{
   public static Serializer Serializer(this ComponentCombination combination) =>
      (Serializer)combination.Components[0];

   public static SqlLayer SqlLayer(this ComponentCombination combination) =>
      (SqlLayer)combination.Components[1];
}
