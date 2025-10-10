using System.Threading.Tasks;
using Compze.Tests.Infrastructure.NUnit.Logging;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

[SetUpFixture] public class UniversalTestFixture
{
   [OneTimeSetUp] public void UniversalSetup()
   {
      TestFixtureHelper.RunAssemblyLevelSetup<UniversalTestFixture>(() =>
      {
         License.Accepted = true;
         TestFixtureHelper.PerformSetup(new NUnitTestEnricher());
         AssertTestInheritsUniversalTestBase();
      });
   }

   [OneTimeTearDown] public async Task UniversalTeardown()
   {
      TestFixtureHelper.RunAssemblyLevelTeardown<UniversalTestFixture>(TestFixtureHelper.PerformTeardown);
      await Task.CompletedTask; // Keep async signature for consistency
   }

   void AssertTestInheritsUniversalTestBase()
   {
      TestFixtureHelper.AssertAllTestClassesInheritFromBase(
         GetType().Assembly,
         typeof(UniversalTestBase),
         TestFixtureHelper.IsNUnitTestClass);
   }
}
