using System.Threading.Tasks;
using Compze.Tests.Infrastructure.NUnit.Logging;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

[SetUpFixture] public class NUnitUniversalTestFixture
{
   [OneTimeSetUp] public void UniversalSetup()
   {
      TestFixtureHelper.RunAssemblyLevelSetup<NUnitUniversalTestFixture>(() =>
      {
         License.Accepted = true;
         AssertTestInheritsUniversalTestBase();
      });
   }

   [OneTimeTearDown] public async Task UniversalTeardown()
   {
      TestFixtureHelper.RunAssemblyLevelTeardown<NUnitUniversalTestFixture>(TestFixtureHelper.PerformTeardown);
      await Task.CompletedTask;
   }

   void AssertTestInheritsUniversalTestBase()
   {
      TestFixtureHelper.AssertAllTestClassesInheritFromUniversalTestBase(
         GetType().Assembly,
         TestFixtureHelper.IsNUnitTestClass);
   }
}
