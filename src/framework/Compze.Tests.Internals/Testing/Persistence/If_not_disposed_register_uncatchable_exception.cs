using System;
using Compze.DependencyInjection;
using Compze.Functional;
using Compze.SystemCE;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.Testing.Persistence;

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

         Assert.Throws<AggregateException>(() => UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions())
               .InnerExceptions.Should().HaveCount(1);
      });
   }
}
