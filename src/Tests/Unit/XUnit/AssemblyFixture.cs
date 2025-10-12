using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

[assembly: AssemblyFixture(typeof(Compze.Tests.Unit.XUnit.XUnitAssemblyFixture))]

namespace Compze.Tests.Unit.XUnit;

// ReSharper disable once MemberCanBeInternal
public sealed class XUnitAssemblyFixture : IAsyncLifetime
{
   public async ValueTask InitializeAsync()
   {
      Tests.Infrastructure.TestFixtureHelper.RunAssemblyLevelSetup<XUnitAssemblyFixture>(() =>
      {
         License.Accepted = true;
         Tests.Infrastructure.TestFixtureHelper.PerformSetup();
         AssertTestInheritsUniversalTestBase();
      });

      await ValueTask.CompletedTask;
   }

   public async ValueTask DisposeAsync()
   {
      Tests.Infrastructure.TestFixtureHelper.RunAssemblyLevelTeardown<XUnitAssemblyFixture>(Tests.Infrastructure.TestFixtureHelper.PerformTeardown);

      await ValueTask.CompletedTask;
   }

   static void AssertTestInheritsUniversalTestBase()
   {
      Tests.Infrastructure.TestFixtureHelper.AssertAllTestClassesInheritFromBase(
         typeof(XUnitAssemblyFixture).Assembly,
         typeof(Tests.Infrastructure.XUnit.UniversalTestBase),
         Tests.Infrastructure.TestFixtureHelper.IsXUnitTestClass);
   }
}
