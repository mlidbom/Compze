using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE.TasksCE;

public class TaskCEExceptionsTests : UniversalTestBase
{
   static async Task FailingMethod()
   {
      await Task.CompletedTask;
      throw new Exception("I broke");
   }

   [XF] public async Task WithAggregateExceptions_throws_taggregate_exception_containing_all_exceptions() =>
      (await InvokingAsync(async () => await Task.WhenAll(Enumerable.Repeat(1, 10).Select(_ => FailingMethod())).WithAggregateExceptions())
            .Must().ThrowAsync<AggregateException>())
     .Which.InnerExceptions.Must().HaveCount(10);
}
