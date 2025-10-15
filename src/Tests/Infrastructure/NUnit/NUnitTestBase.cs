using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

public class NUnitTestBase : UniversalTestBase
{
   [TearDown] public override void SurfaceAnyUncatchableExceptions() => base.SurfaceAnyUncatchableExceptions();
}
