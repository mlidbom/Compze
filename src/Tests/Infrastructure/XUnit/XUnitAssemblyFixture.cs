using Compze.Utilities.SystemCE;
using System;

namespace Compze.Tests.Infrastructure.XUnit;

// ReSharper disable once MemberCanBeInternal
/// <summary>
/// XUnit v2 doesn't support assembly-level fixtures like v3 does.
/// This class is kept for compatibility but won't be used directly.
/// Instead, setup is done via ModuleInitializer in XUnitStartup.cs
/// and teardown should be done in each test class if needed.
/// </summary>
[Obsolete("XUnit v2 doesn't support assembly fixtures. Use test class fixtures or collection fixtures instead.")]
public sealed class XUnitAssemblyFixture : IDisposable
{
   public XUnitAssemblyFixture() => UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();

   public void Dispose() => TestFixtureHelper.PerformTeardown();
}
