using System;
using Compze.Functional;
using Compze.SystemCE;
using Compze.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Compze.Tests.SystemCE;

class StrictlyManagedResourceTests : UniversalTestBase
{
   [Test, NonParallelizable] public void If_not_disposed_register_uncatchable_exception_when_finalizer_runs()
   {
      UncatchableExceptionsGatherer.TestingMonitor.Update(() =>
      {
         Unit.From(() =>
         {
            _ = new StrictlyManagedResource<MyClass>();
         });

         Assert.Throws<AggregateException>(() => UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions())
               .InnerExceptions.Should().HaveCount(1);
      });
   }

   [UsedImplicitly]class MyClass : IStrictlyManagedResource
   {
      public void Dispose() => throw new NotImplementedException();
   }
}
