namespace Compze.xUnitMatrix.Tests._2Components;

static class OurMatrixCombination
{
   public static Serializer Serializer(this MatrixCombination combination) =>
      (Serializer)combination.DimensionValues[0];

   public static SqlLayer SqlLayer(this MatrixCombination combination) =>
      (SqlLayer)combination.DimensionValues[1];
}
