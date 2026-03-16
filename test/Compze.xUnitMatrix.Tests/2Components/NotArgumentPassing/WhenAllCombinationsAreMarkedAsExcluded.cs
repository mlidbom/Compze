using Compze.xUnitMatrix;

namespace Compze.xUnitMatrix.Tests._2Components.NotArgumentPassing;

public class WhenAllPermutationsAreSkipped
{
   public WhenAllPermutationsAreSkipped() => throw new Exception("Should not be executed");

   [NotArgumentPassingTwoComponentsPCT]
   [Skip<Serializer>(Serializer.Microsoft, "TODO")]
   [Skip<Serializer>(Serializer.Newtonsoft, "Not supported")]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");
}
