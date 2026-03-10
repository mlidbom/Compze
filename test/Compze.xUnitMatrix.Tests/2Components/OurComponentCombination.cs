namespace Compze.xUnitMatrix.Tests._2Components;

static class OurMatrixCombination
{
   public static Serializer Serializer(this MatrixCombination combination) =>
      (Serializer)combination.Components[0];

   public static SqlLayer SqlLayer(this MatrixCombination combination) =>
      (SqlLayer)combination.Components[1];
}
