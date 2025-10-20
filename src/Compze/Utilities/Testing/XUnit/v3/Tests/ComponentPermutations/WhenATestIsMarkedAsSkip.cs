using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.ComponentPermutations;

public class WhenATestIsMarkedAsSkip
{
   [PCT(Skip = "test skipping")] public void ItIsNotExecuted() => throw new Exception("This should have been skipped");
}
