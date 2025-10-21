namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class WhenATestIsMarkedAsSkip
{
   public WhenATestIsMarkedAsSkip() => throw new Exception("Constructor should not be called for ignored tests");

   [PCT(Skip = "test skipping")] public void ItIsNotExecuted() => throw new Exception("Ignored test methods should not be executed");
}
