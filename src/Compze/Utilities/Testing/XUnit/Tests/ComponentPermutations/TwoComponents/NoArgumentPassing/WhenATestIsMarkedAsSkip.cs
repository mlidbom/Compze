namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents.NoArgumentPassing;

public class WhenATestIsMarkedAsSkip
{
   public WhenATestIsMarkedAsSkip() => throw new Exception("Constructor should not be called for ignored tests");

   [NoArgumentPassingTwoComponentsPCT(Skip = "test skipping")] public void ItIsNotExecuted() => throw new Exception("Ignored test methods should not be executed");
}
