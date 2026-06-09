using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._4Components;

public class WhenSomethingHappens
{
   [FourComponentMatrix] public void TheCombinationExposesAllFourDimensionValues()
   {
      var combination = MatrixCombination.Current;
      combination.DimensionValues.Must().HaveCount(4);
      FourComponentMatrixAttribute.Serializer.Must().Be((Serializer)combination.DimensionValues[0]);
      FourComponentMatrixAttribute.SqlLayer.Must().Be((SqlLayer)combination.DimensionValues[1]);
      FourComponentMatrixAttribute.DIContainer.Must().Be((DIContainer)combination.DimensionValues[2]);
      FourComponentMatrixAttribute.TeventStore.Must().Be((TeventStore)combination.DimensionValues[3]);
   }
}
