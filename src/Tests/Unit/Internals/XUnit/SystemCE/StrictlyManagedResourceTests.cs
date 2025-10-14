using System;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;
using static FluentAssertions.FluentActions;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Unit.Internals.XUnit.SystemCE;

public class StrictlyManagedResourceTests : XUnitTestBase
{
   //Note: NonParallelizable removed in migration to XUnit. Should things turn flaky...
   [Fact] public void If_not_disposed_register_uncatchable_exception_when_finalizer_runs()
   {
      UncatchableExceptionsGatherer.TestingMonitor.Update(() =>
      {
         unit.From(() =>
         {
            _ = new StrictlyManagedResource<MyClass>();
         });

         Invoking(UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions)
           .Should().Throw<AggregateException>()
           .Which.InnerExceptions.Should().HaveCount(1);
      });
   }

   [UsedImplicitly] class MyClass : IStrictlyManagedResource
   {
      public void Dispose() => throw new NotImplementedException();
   }
}
