using Compze.Tests.Common.Testing.Sql;
using Compze.Tests.Unit.Internals;
using Compze.Utilities.Testing.DbPool.SystemCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Testing.DbPool;
using Compze.Utilities.Testing.Must;
using Xunit;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Tests.Integration.Testing.Sql;

[Collection(nameof(NonParallelCollection))]
public class If_not_disposed_ : DbPoolTestBase
{
   [PCT] public void Register_uncatchable_exception()
   {
      StrictlyManagedResources.SuppressLoggingWhileExecuting(() =>
                                                                UncatchableExceptionsGatherer.TestingMonitor.Locked(() =>
                                                                {
                                                                   unit.From(() =>
                                                                   {
                                                                      _ = CreateServiceLocator().Resolve<DbPool>();
                                                                   });

                                                                   Invoking(UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions)
                                                                     .Must().Throw<AggregateException>().Which
                                                                     .InnerExceptions.Must().HaveCount(1);
                                                                }));
   }
}
