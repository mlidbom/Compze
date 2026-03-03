namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components.NotArgumentPassing;

public class WhenATestIsMarkedAsSkip
{
   public WhenATestIsMarkedAsSkip() => throw new Exception("Constructor should not be called for ignored tests");

   [NotArgumentPassingTwoComponentsPCT(Skip = "test skipping")] public void ItIsNotExecuted() => throw new Exception("Ignored test methods should not be executed");
}
