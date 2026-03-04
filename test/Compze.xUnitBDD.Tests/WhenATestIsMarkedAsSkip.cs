namespace Compze.xUnitBDD.Tests;

public class WhenATestIsMarkedAsSkip
{
   public WhenATestIsMarkedAsSkip() => throw new Exception("Constructor should not be called for ignored tests");
   [XF(Skip = "test skipping")] public void ItIsNotExecuted() => throw new Exception("This should have been skipped");
}
