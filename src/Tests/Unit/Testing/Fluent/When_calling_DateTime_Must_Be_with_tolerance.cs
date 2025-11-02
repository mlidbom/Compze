using System;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must_Be_DateTime = Compze.Utilities.Testing.Fluent.Must_Be_DateTime;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_DateTime_Must_Be_with_tolerance : UniversalTestBase
{
   readonly DateTime _expected = DateTimeCE.ParseInvariant("2025-01-01 12:00:00");
   readonly TimeSpan _tolerance = TimeSpan.FromMinutes(5);

   [XF] public void it_does_not_throw_when_values_are_equal()
      => Must_Be_DateTime.Be(__Must.Must(_expected), _expected, tolerance: _tolerance);

   [XF] public void it_does_not_throw_when_within_tolerance_before()
      => Must_Be_DateTime.Be(__Must.Must(DateTimeCE.ParseInvariant("2025-01-01 11:57:00")), _expected, tolerance: _tolerance);

   [XF] public void it_does_not_throw_when_within_tolerance_after()
      => Must_Be_DateTime.Be(__Must.Must(DateTimeCE.ParseInvariant("2025-01-01 12:03:00")), _expected, tolerance: _tolerance);

   [XF] public void it_does_not_throw_when_exactly_at_tolerance_boundary_before()
      => Must_Be_DateTime.Be(__Must.Must(DateTimeCE.ParseInvariant("2025-01-01 11:55:00")), _expected, tolerance: _tolerance);

   [XF] public void it_does_not_throw_when_exactly_at_tolerance_boundary_after()
      => Must_Be_DateTime.Be(__Must.Must(DateTimeCE.ParseInvariant("2025-01-01 12:05:00")), _expected, tolerance: _tolerance);

   [XF] public void it_throws_when_too_far_before()
      => Invoking(() => Must_Be_DateTime.Be(__Must.Must(DateTimeCE.ParseInvariant("2025-01-01 11:54:59")), _expected, tolerance: _tolerance))
        .Must()
        .Throw<AssertionFailedException>();

   [XF] public void it_throws_when_too_far_after()
      => Invoking(() => Must_Be_DateTime.Be(__Must.Must(DateTimeCE.ParseInvariant("2025-01-01 12:05:01")), _expected, tolerance: _tolerance))
        .Must()
        .Throw<AssertionFailedException>();
}
