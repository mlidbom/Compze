using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.TestInfrastructure;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;
using Compze.TestInfrastructure.NUnit;

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE.TasksCE;

public class TaskCEExceptionsTests : UniversalTestBase
{
   static async Task FailingMethod()
   {
      await Task.CompletedTask;
      throw new Exception("I broke");
   }

   [Test] public async Task WithAggregateExceptions_throws_aggregate_exception_containing_all_exceptions() =>
      (await Invoking(async () => await Task.WhenAll(Enumerable.Repeat(1, 10).Select(_ => FailingMethod())).WithAggregateExceptions())
            .Should().ThrowAsync<AggregateException>())
     .Which.InnerExceptions.Should().HaveCount(10);
}
