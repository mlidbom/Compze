using Compze.DbPool.SystemCE;
using Compze.DependencyInjection;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;

using Compze.SystemCE;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.DbPool.Tests;

[Collection(nameof(NonParallelCollection))]
public class If_not_disposed_ : DbPoolTestBase
{
   [PCT] public void Register_uncatchable_exception()
   {
      StrictlyManagedResources.SuppressLoggingWhileExecuting(() =>
                                                                UncatchableExceptionsGatherer.TestingMonitor.Locked(() =>
                                                                {
                                                                   Unit.Invoke(() =>
                                                                   {
                                                                      _ = CreateContainer().Resolve<DbPool>();
                                                                   });

                                                                   Invoking(UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions)
                                                                     .Must().Throw<AggregateException>().Which
                                                                     .InnerExceptions.Must().HaveCount(1);
                                                                }));
   }
}

