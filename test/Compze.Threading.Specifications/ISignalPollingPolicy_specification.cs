using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications;

public class ISignalPollingPolicy_specification : UniversalTestBase
{
   public class The_Default_policy : ISignalPollingPolicy_specification
   {
      readonly ISignalPollingPolicy _policy = ISignalPollingPolicy.Default;

      [XF] public void polls_after_1_millisecond_at_the_start_of_a_wait() =>
         _policy.NextPollInterval(TimeSpan.Zero).Must().Be(TimeSpan.FromMilliseconds(1));

      [XF] public void sleeps_a_quarter_of_the_time_waited_so_far() =>
         _policy.NextPollInterval(TimeSpan.FromMilliseconds(100)).Must().Be(TimeSpan.FromMilliseconds(25));

      [XF] public void never_sleeps_longer_than_the_50_millisecond_max_signal_latency_target() =>
         _policy.NextPollInterval(TimeSpan.FromHours(1)).Must().Be(TimeSpan.FromMilliseconds(50));
   }

   public class WithMaxSignalLatency_ : ISignalPollingPolicy_specification
   {
      [XF] public void caps_poll_intervals_at_the_target() =>
         ISignalPollingPolicy.WithMaxSignalLatency(TimeSpan.FromMilliseconds(10)).NextPollInterval(TimeSpan.FromHours(1)).Must().Be(TimeSpan.FromMilliseconds(10));

      [XF] public void still_polls_after_1_millisecond_at_the_start_of_a_wait() =>
         ISignalPollingPolicy.WithMaxSignalLatency(TimeSpan.FromMilliseconds(500)).NextPollInterval(TimeSpan.Zero).Must().Be(TimeSpan.FromMilliseconds(1));

      [XF] public void rejects_a_target_below_the_1_millisecond_minimum_poll_interval() =>
         Invoking(() => ISignalPollingPolicy.WithMaxSignalLatency(TimeSpan.FromMilliseconds(0.5))).Must().Throw<ArgumentAssertionFailedException>();
   }
}
