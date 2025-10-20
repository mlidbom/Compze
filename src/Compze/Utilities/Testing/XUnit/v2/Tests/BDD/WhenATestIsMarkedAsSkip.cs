using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Utilities.Testing.XUnit.v2.Tests.BDD;

public class WhenATestIsMarkedAsSkip
{
   [XF(Skip = "test skipping")] public void ItIsNotExecuted() => throw new Exception("This should have been skipped");
}
