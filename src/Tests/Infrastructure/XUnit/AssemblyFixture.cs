using System;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure.XUnit;
using Xunit;

[assembly: AssemblyFixture(typeof(XUnitAssemblyFixture))]

namespace Compze.Tests.Infrastructure.XUnit;

// ReSharper disable once MemberCanBeInternal
public sealed class XUnitAssemblyFixture : IAsyncLifetime
{
   public async ValueTask InitializeAsync()
   {
      TestFixtureHelper.RunAssemblyLevelTeardown<XUnitAssemblyFixture>(TestFixtureHelper.PerformTeardown);
      await ValueTask.CompletedTask;
      throw new Exception("Verifying that we get here: InitializeAsync");
   }

   //XUnit does NOT surface exceptions thrown here it seems, and they might be causing issues in NCrunch so don't check.
   public async ValueTask DisposeAsync() => await ValueTask.CompletedTask;
}
