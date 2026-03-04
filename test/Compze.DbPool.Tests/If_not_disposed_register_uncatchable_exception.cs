using Compze.DbPool.SystemCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.Must;
using Xunit;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.DbPool.Tests;

[Collection(nameof(NonParallelCollection))]
public class If_not_disposed_ : DbPoolTestBase
{
   [PCT] public void Register_uncatchable_exception()
   {
      StrictlyManagedResources.SuppressLoggingWhileExecuting(() =>
                                                                UncatchableExceptionsGatherer.TestingMonitor.Locked(() =>
                                                                {
                                                                   unit.Invoke(() =>
                                                                   {
                                                                      _ = CreateServiceLocator().Resolve<global::Compze.DbPool.DbPool>();
                                                                   });

                                                                   Invoking(UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions)
                                                                     .Must().Throw<AggregateException>().Which
                                                                     .InnerExceptions.Must().HaveCount(1);
                                                                }));
   }
}
