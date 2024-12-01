using Compze.SystemCE;
using NUnit.Framework;

namespace Compze.Testing;

public class UniversalTestBase
{
   //[TearDown] public void TearDown() => UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
   [TearDown] public void SurfaceAnyUncatchableExceptions() => UncatchableExceptionsGatherer.ConsumeAndThrowAnyExceptionsGathered();
}
