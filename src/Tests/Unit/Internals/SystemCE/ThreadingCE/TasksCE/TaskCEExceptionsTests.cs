using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using static FluentAssertions.FluentActions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE.TasksCE;

public class TaskCEExceptionsTests : XUnitTestBase
{
   static async Task FailingMethod()
   {
      await Task.CompletedTask;
      throw new Exception("I broke");
   }

   [XF] public async Task WithAggregateExceptions_throws_aggregate_exception_containing_all_exceptions() =>
      (await Invoking(async () => await Task.WhenAll(Enumerable.Repeat(1, 10).Select(_ => FailingMethod())).WithAggregateExceptions())
            .Should().ThrowAsync<AggregateException>())
     .Which.InnerExceptions.Should().HaveCount(10);
}
