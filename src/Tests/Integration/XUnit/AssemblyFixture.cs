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
   public async ValueTask InitializeAsync() => await ValueTask.CompletedTask;

   public async ValueTask DisposeAsync()
   {
      TestFixtureHelper.RunAssemblyLevelTeardown<XUnitAssemblyFixture>(TestFixtureHelper.PerformTeardown);
      await ValueTask.CompletedTask;
   }
}
