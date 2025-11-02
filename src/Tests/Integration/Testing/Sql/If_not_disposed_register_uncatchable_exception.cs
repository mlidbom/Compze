using System;
using Compze.Tests.Common.Testing.Sql;
using Compze.Tests.Unit.Internals;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Testing.DbPool;
using Compze.Tests.Infrastructure.Fluent;
using Xunit;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

namespace Compze.Tests.Integration.Testing.Sql;

[Collection(nameof(NonParallelCollection))]
public class If_not_disposed_ : DbPoolTestBase
{
   [PCT] public void Register_uncatchable_exception()
   {
      StrictlyManagedResources.SuppressLoggingWhileExecuting(() =>
                                                                UncatchableExceptionsGatherer.TestingMonitor.Update(() =>
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
