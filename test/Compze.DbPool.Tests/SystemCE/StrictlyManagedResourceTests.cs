using Compze.DbPool.SystemCE;
using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.SystemCE;
using Compze.xUnitBDD;
using JetBrains.Annotations;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.DbPool.Tests.SystemCE;

[Collection(nameof(NonParallelCollection))]
public class StrictlyManagedResourceTests : UniversalTestBase
{
   //Note: NonParallelizable removed in migration to XUnit. Should things turn flaky...
   [XF] public void If_not_disposed_register_uncatchable_exception_when_finalizer_runs()
   {
      StrictlyManagedResources.SuppressLoggingWhileExecuting(() =>
                                                                UncatchableExceptionsGatherer.TestingMonitor.Locked(() =>
                                                                {
                                                                   Unit.Invoke(() =>
                                                                   {
                                                                      _ = new StrictlyManagedResource<MyClass>();
                                                                   });

                                                                   Invoking(UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions)
                                                                     .Must().Throw<AggregateException>()
                                                                     .Which.InnerExceptions.Must().HaveCount(1);
                                                                }));
   }
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
   [UsedImplicitly] class MyClass : IStrictlyManagedResource
   {
      public void Dispose() => throw new NotImplementedException();
   }
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
}
