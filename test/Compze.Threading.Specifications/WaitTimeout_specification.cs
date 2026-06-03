using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.Threading.Specifications;

public class WaitTimeout_specification : UniversalTestBase
{
   public class Factory_methods : WaitTimeout_specification
   {
      [XF] public void Milliseconds_from_long_creates_timeout_with_correct_value() =>
         WaitTimeout.Milliseconds(500L).Value.Must().Be(TimeSpan.FromMilliseconds(500));

      [XF] public void Milliseconds_from_double_creates_timeout_with_correct_value() =>
         WaitTimeout.Milliseconds(500.5).Value.Must().Be(TimeSpan.FromMilliseconds(500.5));

      [XF] public void Seconds_from_long_creates_timeout_with_correct_value() =>
         WaitTimeout.Seconds(7L).Value.Must().Be(TimeSpan.FromSeconds(7));

      [XF] public void Seconds_from_double_creates_timeout_with_correct_value() =>
         WaitTimeout.Seconds(7.5).Value.Must().Be(TimeSpan.FromSeconds(7.5));

      [XF] public void Minutes_from_long_creates_timeout_with_correct_value() =>
         WaitTimeout.Minutes(3L).Value.Must().Be(TimeSpan.FromMinutes(3));

      [XF] public void Minutes_from_double_creates_timeout_with_correct_value() =>
         WaitTimeout.Minutes(3.5).Value.Must().Be(TimeSpan.FromMinutes(3.5));
   }

   public class Infinite_ : WaitTimeout_specification
   {
      [XF] public void has_InfiniteTimeSpan_value() =>
         WaitTimeout.Infinite.Value.Must().Be(Timeout.InfiniteTimeSpan);

      [XF] public void IsInfinite_is_true() =>
         WaitTimeout.Infinite.IsInfinite.Must().BeTrue();

      [XF] public void Default_is_Infinite() =>
         WaitTimeout.Default.Must().Be(WaitTimeout.Infinite);
   }

   public class IsInfinite_ : WaitTimeout_specification
   {
      [XF] public void returns_false_for_finite_timeout() =>
         WaitTimeout.Seconds(5).IsInfinite.Must().BeFalse();

      [XF] public void returns_true_for_Infinite() =>
         WaitTimeout.Infinite.IsInfinite.Must().BeTrue();
   }

   public class Equality : WaitTimeout_specification
   {
      [XF] public void equal_values_are_equal() =>
         WaitTimeout.Seconds(5).Must().Be(WaitTimeout.Seconds(5));

      [XF] public void different_values_are_not_equal() =>
         WaitTimeout.Seconds(5).Must().NotBe(WaitTimeout.Seconds(10));

      [XF] public void equality_operator_returns_true_for_equal_values() =>
         // ReSharper disable once EqualExpressionComparison Two equal-but-distinct instances — exercising the == operator is the point of this spec.
         (WaitTimeout.Seconds(5) == WaitTimeout.Seconds(5)).Must().BeTrue();

      [XF] public void inequality_operator_returns_true_for_different_values() =>
         (WaitTimeout.Seconds(5) != WaitTimeout.Seconds(10)).Must().BeTrue();

      [XF] public void Equals_with_boxed_value_returns_true_for_equal_value() =>
         WaitTimeout.Seconds(5).Equals((object)WaitTimeout.Seconds(5)).Must().BeTrue();

      [XF] public void Equals_with_boxed_value_returns_false_for_different_type() =>
         // ReSharper disable once SuspiciousTypeConversion.Global Deliberately comparing to a different type — verifying Equals returns false for a non-WaitTimeout is the point of this spec.
         WaitTimeout.Seconds(5).Equals("not a timeout").Must().BeFalse();

      [XF] public void equal_values_have_the_same_hash_code() =>
         WaitTimeout.Seconds(5).GetHashCode().Must().Be(WaitTimeout.Seconds(5).GetHashCode());
   }

   public class Conversion : WaitTimeout_specification
   {
      [XF] public void implicitly_converts_to_TimeSpan() =>
         ((TimeSpan)WaitTimeout.Seconds(5)).Must().Be(TimeSpan.FromSeconds(5));

      [XF] public void ToTimeSpan_returns_the_underlying_value() =>
         WaitTimeout.Seconds(5).ToTimeSpan().Must().Be(TimeSpan.FromSeconds(5));
   }

   public class ToString_ : WaitTimeout_specification
   {
      [XF] public void returns_the_TimeSpan_string_representation() =>
         WaitTimeout.Seconds(5).ToString().Must().Be(TimeSpan.FromSeconds(5).ToString());
   }
}
