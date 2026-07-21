using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Testing.Specifications;

[Collection(nameof(NonParallelCollection))]
public class Given_an_open_ThreadGate : UniversalTestBase
{
   readonly IThreadGate _gate = IThreadGate.NewOpen(WaitTimeout.Milliseconds(20), "testGate");

   [XF] public void its_WaitTimeout_is_the_timeout_it_was_created_with() => _gate.WaitTimeout.Must().Be(WaitTimeout.Milliseconds(20));

   [XF] public void Calling_AwaitClosed_throws_an_AwaitingConditionTimeoutException_when_the_WaitTimeout_expires_because_nothing_closes_the_gate()
      => Invoking(() => _gate.AwaitClosed()).Must().Throw<AwaitingConditionTimeoutException>();

   [XF] public void Calling_AwaitClosed_returns_once_the_gate_is_closed()
   {
      _gate.Close();
      _gate.AwaitClosed();
   }
}
