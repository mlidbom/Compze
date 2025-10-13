using System.Threading.Tasks;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

[SetUpFixture] public class NUnitUniversalTestFixture
{
   [OneTimeTearDown] public async Task UniversalTeardown()
   {
      TestFixtureHelper.RunAssemblyLevelTeardown<NUnitUniversalTestFixture>(TestFixtureHelper.PerformTeardown);
      await Task.CompletedTask;
   }
}
