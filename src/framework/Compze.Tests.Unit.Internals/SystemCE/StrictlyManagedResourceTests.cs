using System;
using Compze.SystemCE;
using Compze.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.Internals.SystemCE;

class StrictlyManagedResourceTests : UniversalTestBase
{
   [Test, NonParallelizable] public void If_not_disposed_register_uncatchable_exception_when_finalizer_runs()
   {
      UncatchableExceptionsGatherer.TestingMonitor.Update(() =>
      {
         Functional.Unit.From(() =>
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
