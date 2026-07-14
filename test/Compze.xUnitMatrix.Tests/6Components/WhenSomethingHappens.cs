using Compze.Must.Assertions;
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._6Components;

public class WhenSomethingHappens
{
   [SixComponentMatrix] public void TheCombinationExposesAllSixDimensionValues()
   {
      var combination = MatrixCombination.Current;
      combination.DimensionValues.Must().HaveCount(6);
      SixComponentMatrixAttribute.Serializer.Must().Be((Serializer)combination.DimensionValues[0]);
      SixComponentMatrixAttribute.SqlLayer.Must().Be((SqlLayer)combination.DimensionValues[1]);
      SixComponentMatrixAttribute.DIContainer.Must().Be((DIContainer)combination.DimensionValues[2]);
      SixComponentMatrixAttribute.TeventStore.Must().Be((TeventStore)combination.DimensionValues[3]);
      SixComponentMatrixAttribute.TessageBus.Must().Be((TessageBus)combination.DimensionValues[4]);
      SixComponentMatrixAttribute.Transport.Must().Be((Transport)combination.DimensionValues[5]);
   }
}
