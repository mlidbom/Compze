using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.SystemCE.ThreadingCE.TasksCE;

public class TaskCEExceptionsTests : UniversalTestBase
{
   static async Task FailingMethod()
   {
      await Task.CompletedTask.CaF();
      throw new Exception("I broke");
   }

   [Test] public async Task WithAggregateExceptions_throws_aggregate_exception_containing_all_exceptions() =>
      (await AssertThrows.Async<AggregateException>(async () => await Task.WhenAll(Enumerable.Repeat(1, 10).Select(_ => FailingMethod())).WithAggregateExceptions().CaF()).CaF())
     .InnerExceptions.Should().HaveCount(10);
}
