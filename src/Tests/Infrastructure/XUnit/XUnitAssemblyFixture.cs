using Compze.Utilities.SystemCE;
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
   public async ValueTask DisposeAsync()
   {
      UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
      await ValueTask.CompletedTask;
   }

   public async ValueTask InitializeAsync()
   {
      TestFixtureHelper.PerformTeardown();
      await ValueTask.CompletedTask;
   }
}
