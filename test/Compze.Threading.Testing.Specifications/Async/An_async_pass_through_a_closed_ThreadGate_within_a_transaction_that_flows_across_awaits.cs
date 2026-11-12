using System.Diagnostics;
using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Testing.Specifications.Async;

[Collection(nameof(NonParallelCollection))]
public class An_async_pass_through_a_closed_ThreadGate_within_a_transaction_that_flows_across_awaits : UniversalTestBase
{
   readonly IThreadGate _gate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "gate");
   bool _thePostPassThroughActionSawATransaction;

   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      _gate.SetPostPassThroughAction(_ => _thePostPassThroughActionSawATransaction = Transaction.Current != null);

      using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
      var pass = _gate.AwaitPassThroughAsync();
      await _gate.AwaitQueueLengthEqualToAsync(1);
      _gate.Open();
      await pass.WaitAsync(TimeSpan.FromSeconds(10));
   }

   [XF] public void the_post_pass_through_action_sees_the_callers_transaction() => _thePostPassThroughActionSawATransaction.Must().BeTrue();
}
