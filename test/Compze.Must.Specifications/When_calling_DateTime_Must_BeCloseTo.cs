

// ReSharper disable InconsistentNaming



namespace Compze.Must.Specifications;

public class When_calling_DateTime_Must_BeCloseTo : UniversalTestBase
{
   readonly DateTime _expected = DateTimeCE.ParseInvariant("2025-01-01 12:00:00");
   readonly TimeSpan _tolerance = TimeSpan.FromMinutes(5);

   [XF] public void it_does_not_throw_when_values_are_equal()
      => _expected.Must().BeCloseTo(_expected, tolerance: _tolerance);

   [XF] public void it_does_not_throw_when_within_tolerance_before()
      => DateTimeCE.ParseInvariant("2025-01-01 11:57:00").Must().BeCloseTo(_expected, tolerance: _tolerance);

   [XF] public void it_does_not_throw_when_within_tolerance_after()
      => DateTimeCE.ParseInvariant("2025-01-01 12:03:00").Must().BeCloseTo(_expected, tolerance: _tolerance);

   [XF] public void it_does_not_throw_when_exactly_at_tolerance_boundary_before()
      => DateTimeCE.ParseInvariant("2025-01-01 11:55:00").Must().BeCloseTo(_expected, tolerance: _tolerance);

   [XF] public void it_does_not_throw_when_exactly_at_tolerance_boundary_after()
      => DateTimeCE.ParseInvariant("2025-01-01 12:05:00").Must().BeCloseTo(_expected, tolerance: _tolerance);

   [XF] public void it_throws_when_too_far_before()
      => Invoking(() => DateTimeCE.ParseInvariant("2025-01-01 11:54:59").Must().BeCloseTo(_expected, tolerance: _tolerance))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_when_too_far_after()
      => Invoking(() => DateTimeCE.ParseInvariant("2025-01-01 12:05:01").Must().BeCloseTo(_expected, tolerance: _tolerance))
        .Must()
        .Throw<AssertionFailedException>();
}
