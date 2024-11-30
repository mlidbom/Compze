﻿using System;
using Composable.DependencyInjection;
using Composable.Functional;
using Composable.SystemCE;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests;

class If_not_disposed_(string pluggableComponentsCombination) : DbPoolTest(pluggableComponentsCombination)
{
   [Test] public void Register_uncatchable_exception()
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
