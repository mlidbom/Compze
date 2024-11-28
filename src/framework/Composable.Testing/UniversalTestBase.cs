using Composable.SystemCE;
using NUnit.Framework;

namespace Composable.Testing;

public class UniversalTestBase
{
   //[TearDown] public void TearDown() => UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
   [TearDown] public void TearDown() => UncatchableExceptionsGatherer.ConsumeAndThrowAnyExceptionsGathered();
}
