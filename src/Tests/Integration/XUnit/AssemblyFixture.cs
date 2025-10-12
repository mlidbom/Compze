using System;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

[assembly: AssemblyFixture(typeof(Compze.Tests.Integration.XUnit.XUnitAssemblyFixture))]

namespace Compze.Tests.Integration.XUnit;

// ReSharper disable once MemberCanBeInternal
public sealed class XUnitAssemblyFixture : IAsyncLifetime
{
   public async ValueTask InitializeAsync()
   {
      TestFixtureHelper.RunAssemblyLevelSetup<XUnitAssemblyFixture>(() =>
      {
         License.Accepted = true;
         TestFixtureHelper.PerformSetup();
         AssertTestInheritsUniversalTestBase();
      });

      await ValueTask.CompletedTask;
   }

   public async ValueTask DisposeAsync()
   {
      TestFixtureHelper.RunAssemblyLevelTeardown<XUnitAssemblyFixture>(TestFixtureHelper.PerformTeardown);
      await ValueTask.CompletedTask;
   }

   static void AssertTestInheritsUniversalTestBase()
   {
      TestFixtureHelper.AssertAllTestClassesInheritFromBase(
         typeof(XUnitAssemblyFixture).Assembly,
         typeof(Tests.Infrastructure.XUnit.UniversalTestBase),
         TestFixtureHelper.IsXUnitTestClass);
   }
}
