using System;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Integration.Internals.Testing.Sql;

class If_not_disposed_(string pluggableComponentsCombination) : NUnitDbPoolTest(pluggableComponentsCombination)
{
   [Test, NonParallelizable] public void Register_uncatchable_exception()
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
