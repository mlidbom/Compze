using Compze.Utilities.SystemCE;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

public class UniversalTestBase
{
   //[TearDown] public void TearDown() => UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
   [TearDown] public void SurfaceAnyUncatchableExceptions() => UncatchableExceptionsGatherer.ConsumeAndThrowAnyExceptionsGathered();
}
