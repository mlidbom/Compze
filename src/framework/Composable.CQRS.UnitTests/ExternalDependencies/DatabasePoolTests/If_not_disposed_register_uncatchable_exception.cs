using Composable.Functional;
using Composable.SystemCE;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests;

class If_not_disposed_(string pluggableComponentsCombination) : DbPoolTest(pluggableComponentsCombination)
{
   [Test] public void Register_uncatchable_exception()
   {
      UncatchableExceptionsGatherer.TestingMonitor.Update(() =>
      {
         Unit.From(() =>
         {
            _ = CreatePool();
         });

         UncatchableExceptionsGatherer.ForceGcCollectionWaitForFinalizersAndConsumeErrors().Should().HaveCount(1);
      });
   }
}
