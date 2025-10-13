using System;
using System.Threading.Tasks;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit;

// ReSharper disable once MemberCanBeInternal
/// <summary>
/// put the below in AssemblyFixture.cs in each test project using XUnit 
///[assembly: AssemblyFixture(typeof(XUnitAssemblyFixture))]
/// </summary>
public sealed class XUnitAssemblyFixture : IAsyncLifetime
{
   public async ValueTask InitializeAsync()
   {
      TestFixtureHelper.RunAssemblyLevelTeardown<XUnitAssemblyFixture>(TestFixtureHelper.PerformTeardown);
      await ValueTask.CompletedTask;
   }

   //XUnit does NOT surface exceptions thrown here it seems, and they might be causing issues in NCrunch so don't check.
   public async ValueTask DisposeAsync() => await ValueTask.CompletedTask;
}
