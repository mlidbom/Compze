namespace Compze.xUnitMatrix.Tests._2Components;

public class WhenAllCombinationsAreSkipped
{
   public WhenAllCombinationsAreSkipped() => throw new Exception("Should not be executed");

   [TwoComponentMatrix]
   [Skip<Serializer>(Serializer.Microsoft, "TODO")]
   [Skip<Serializer>(Serializer.Newtonsoft, "Not supported")]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");
}
