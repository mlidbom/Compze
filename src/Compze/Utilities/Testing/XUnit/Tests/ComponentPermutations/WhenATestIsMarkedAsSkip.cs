using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenATestIsMarkedAsSkip
{
   public WhenATestIsMarkedAsSkip() => throw new Exception("Constructor should not be called for ignored tests");

   [TypedPCT(Skip = "test skipping")] public void ItIsNotExecuted() => throw new Exception("Ignored test methods should not be executed");
}
