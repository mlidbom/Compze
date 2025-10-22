namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAllPermutationsAreSkipped
{
   public WhenAllPermutationsAreSkipped() => throw new Exception("Should not be executed");

   [OurPCT(
      skipped: [Serializer.Microsoft, Serializer.Newtonsoft],
      skipReasons: ["TODO", "Not supported"])]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");
}
