using System;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Utilities.Tests.Testing.Xunit.BDD;

public class WhenATestIsMarkedAsSkip
{
   public WhenATestIsMarkedAsSkip() => throw new Exception("Constructor should not be called for ignored tests");
   [XF(Skip = "test skipping")] public void ItIsNotExecuted() => throw new Exception("This should have been skipped");
}
