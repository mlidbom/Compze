using System;
using Composable.Functional;
using Composable.SystemCE;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.SystemCE;

class StrictlyManagedResourceTests
{
   [Test] public void If_not_disposed_register_uncatchable_exception_when_finalizer_runs()
   {
      UncatchableExceptionsGatherer.TestingMonitor.Update(() =>
      {
         Unit.From(() =>
         {
            _ = new StrictlyManagedResource<MyClass>();
         });

         UncatchableExceptionsGatherer.ForceGcCollectionWaitForFinalizersAndConsumeErrors().Should().HaveCount(1);
      });
   }

   class MyClass : IStrictlyManagedResource
   {
      public void Dispose() => throw new NotImplementedException();
   }
}
