using System;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Base class for all tests that performs lazy initialization on first test construction.
/// This ensures GC and exception gathering happens only when tests actually run, not during assembly load.
/// This class is internal to avoid ambiguous references with framework-specific UniversalTestBase classes.
/// </summary>
public abstract class UniversalTestBase : IDisposable
{
   static readonly RunJustOnce OneTimeAssertion = new(UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions);
   protected UniversalTestBase() => OneTimeAssertion.RunIfNotExecutedBefore();

   public virtual void SurfaceAnyUncatchableExceptions() => UncatchableExceptionsGatherer.ConsumeAndThrowAnyExceptionsGathered();
   public virtual void Dispose() => SurfaceAnyUncatchableExceptions();
}
