namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents.NoArgumentPassing;

public class WhenAllPermutationsAreSkipped
{
   public WhenAllPermutationsAreSkipped() => throw new Exception("Should not be executed");

   [NoArgumentPassingTwoComponentsPCT(
      skipped: [Serializer.Microsoft, Serializer.Newtonsoft],
      skipReasons: ["TODO", "Not supported"])]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");
}
