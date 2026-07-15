
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._5Components;

public class WhenSomethingHappens
{
   [FiveComponentMatrix] public void TheCombinationExposesAllFiveDimensionValues()
   {
      var combination = MatrixCombination.Current;
      combination.DimensionValues.Must().HaveCount(5);
      FiveComponentMatrixAttribute.Serializer.Must().Be((Serializer)combination.DimensionValues[0]);
      FiveComponentMatrixAttribute.SqlLayer.Must().Be((SqlLayer)combination.DimensionValues[1]);
      FiveComponentMatrixAttribute.DIContainer.Must().Be((DIContainer)combination.DimensionValues[2]);
      FiveComponentMatrixAttribute.TeventStore.Must().Be((TeventStore)combination.DimensionValues[3]);
      FiveComponentMatrixAttribute.TessageBus.Must().Be((TessageBus)combination.DimensionValues[4]);
   }
}
