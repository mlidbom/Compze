using Compze.Utilities.SystemCE;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Compze.Tests.Infrastructure.NUnit;

[SetUpFixture] public class NUnitUniversalTestFixture
{
   [OneTimeTearDown] public async Task UniversalTeardown()
   {
      UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
      await Task.CompletedTask;
   }
}
