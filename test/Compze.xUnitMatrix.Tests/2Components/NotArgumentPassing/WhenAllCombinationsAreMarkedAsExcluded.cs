namespace Compze.xUnitMatrix.Tests._2Components.NotArgumentPassing;

public class WhenAllPermutationsAreSkipped
{
   public WhenAllPermutationsAreSkipped() => throw new Exception("Should not be executed");

   [NotArgumentPassingTwoComponentsPCT(Skipped = [Serializer.Microsoft, Serializer.Newtonsoft],
                                       SkipReasons = ["TODO", "Not supported"])]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");
}
