using System;
using Compze.DependencyInjection;
using Compze.Functional;
using Compze.SystemCE;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Integration.Internals.Testing.Persistence;

class If_not_disposed_(string pluggableComponentsCombination) : DbPoolTest(pluggableComponentsCombination)
{
   [Test, NonParallelizable] public void Register_uncatchable_exception()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
      UncatchableExceptionsGatherer.TestingMonitor.Update(() =>
      {
         Unit.From(() =>
         {
            _ = CreatePool();
         });

         Invoking(() => UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions())
           .Should().Throw<AggregateException>().Which
           .InnerExceptions.Should().HaveCount(1);
      });
   }
}
