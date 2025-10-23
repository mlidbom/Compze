using System;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components.NotArgumentPassing;

public class WhenAllPermutationsAreSkipped
{
   public WhenAllPermutationsAreSkipped() => throw new Exception("Should not be executed");

   [NotArgumentPassingTwoComponentsPCT(
      skipped: [Serializer.Microsoft, Serializer.Newtonsoft],
      skipReasons: ["TODO", "Not supported"])]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");
}
