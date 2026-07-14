using Compze.Must;
using Compze.Must.Assertions;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications;

public class LockTimeout_specification : UniversalTestBase
{
   public class Factory_methods : LockTimeout_specification
   {
      [XF] public void Milliseconds_from_long_creates_timeout_with_correct_value() =>
         LockTimeout.Milliseconds(500L).Value.Must().Be(TimeSpan.FromMilliseconds(500));

      [XF] public void Milliseconds_from_double_creates_timeout_with_correct_value() =>
         LockTimeout.Milliseconds(500.5).Value.Must().Be(TimeSpan.FromMilliseconds(500.5));

      [XF] public void Seconds_from_long_creates_timeout_with_correct_value() =>
         LockTimeout.Seconds(7L).Value.Must().Be(TimeSpan.FromSeconds(7));

      [XF] public void Seconds_from_double_creates_timeout_with_correct_value() =>
         LockTimeout.Seconds(7.5).Value.Must().Be(TimeSpan.FromSeconds(7.5));

      [XF] public void Minutes_from_long_creates_timeout_with_correct_value() =>
         LockTimeout.Minutes(3L).Value.Must().Be(TimeSpan.FromMinutes(3));

      [XF] public void Minutes_from_double_creates_timeout_with_correct_value() =>
         LockTimeout.Minutes(3.5).Value.Must().Be(TimeSpan.FromMinutes(3.5));

      [XF] public void Zero_has_zero_duration() =>
         LockTimeout.Zero.Value.Must().Be(TimeSpan.Zero);
   }

   public class Constructor_validation : LockTimeout_specification
   {
      [XF] public void throws_on_InfiniteTimeSpan() =>
         Invoking(() => new LockTimeout(Timeout.InfiniteTimeSpan)).Must().Throw<ArgumentException>();
   }

   public class Equality : LockTimeout_specification
   {
      [XF] public void equal_values_are_equal() =>
         LockTimeout.Seconds(5).Must().Be(LockTimeout.Seconds(5));

      [XF] public void different_values_are_not_equal() =>
         LockTimeout.Seconds(5).Must().NotBe(LockTimeout.Seconds(10));

      [XF] public void equality_operator_returns_true_for_equal_values() =>
         // ReSharper disable once EqualExpressionComparison Two equal-but-distinct instances — exercising the == operator is the point of this spec.
         (LockTimeout.Seconds(5) == LockTimeout.Seconds(5)).Must().BeTrue();

      [XF] public void inequality_operator_returns_true_for_different_values() =>
         (LockTimeout.Seconds(5) != LockTimeout.Seconds(10)).Must().BeTrue();

      [XF] public void Equals_with_boxed_value_returns_true_for_equal_value() =>
         LockTimeout.Seconds(5).Equals((object)LockTimeout.Seconds(5)).Must().BeTrue();

      [XF] public void Equals_with_boxed_value_returns_false_for_different_type() =>
         // ReSharper disable once SuspiciousTypeConversion.Global Deliberately comparing to a different type — verifying Equals returns false for a non-LockTimeout is the point of this spec.
         LockTimeout.Seconds(5).Equals("not a timeout").Must().BeFalse();

      [XF] public void equal_values_have_the_same_hash_code() =>
         LockTimeout.Seconds(5).GetHashCode().Must().Be(LockTimeout.Seconds(5).GetHashCode());
   }

   public class Conversion : LockTimeout_specification
   {
      [XF] public void implicitly_converts_to_TimeSpan() =>
         ((TimeSpan)LockTimeout.Seconds(5)).Must().Be(TimeSpan.FromSeconds(5));

      [XF] public void ToTimeSpan_returns_the_underlying_value() =>
         LockTimeout.Seconds(5).ToTimeSpan().Must().Be(TimeSpan.FromSeconds(5));
   }

   public class ToString_ : LockTimeout_specification
   {
      [XF] public void returns_the_TimeSpan_string_representation() =>
         LockTimeout.Seconds(5).ToString().Must().Be(TimeSpan.FromSeconds(5).ToString());
   }
}
