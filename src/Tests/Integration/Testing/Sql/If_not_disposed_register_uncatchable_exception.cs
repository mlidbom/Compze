using System;
using Compze.Tests.Common.Testing.Sql;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Tests.Unit.Internals;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using FluentAssertions;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Integration.Testing.Sql;

[Collection(nameof(NonParallelCollection))]
public class If_not_disposed_ : DbPoolTestBase
{
   [PCT] public void Register_uncatchable_exception()
   {
      UncatchableExceptionsGatherer.TestingMonitor.Update(() =>
      {
         unit.From(() =>
         {
            _ = CreatePool();
         });

         Invoking(UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions)
           .Should().Throw<AggregateException>().Which
           .InnerExceptions.Should().HaveCount(1);
      });
   }
}
