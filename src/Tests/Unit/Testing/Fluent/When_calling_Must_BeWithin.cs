using System;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BeWithin : UniversalTestBase
{
   public class Before : When_calling_Must_BeWithin
   {
      readonly DateTime _reference = DateTimeCE.ParseInvariant("2025-01-01 12:00:00");

      [XF] public void it_does_not_throw_when_exactly_at_reference() 
         => _reference.Must().BeWithin(TimeSpan.FromMinutes(5)).Before(_reference);

      [XF] public void it_does_not_throw_when_within_tolerance_before_reference()
         => DateTimeCE.ParseInvariant("2025-01-01 11:57:00").Must().BeWithin(TimeSpan.FromMinutes(5)).Before(_reference);

      [XF] public void it_does_not_throw_when_exactly_at_tolerance_boundary()
         => DateTimeCE.ParseInvariant("2025-01-01 11:55:00").Must().BeWithin(TimeSpan.FromMinutes(5)).Before(_reference);

      [XF] public void it_throws_when_too_far_before_reference()
         => Invoking(() => DateTimeCE.ParseInvariant("2025-01-01 11:54:59").Must().BeWithin(TimeSpan.FromMinutes(5)).Before(_reference))
           .Must()
           .Throw<AssertionFailedException>();

      [XF] public void it_throws_when_after_reference()
         => Invoking(() => DateTimeCE.ParseInvariant("2025-01-01 12:00:01").Must().BeWithin(TimeSpan.FromMinutes(5)).Before(_reference))
           .Must()
           .Throw<AssertionFailedException>();
   }

   public class After : When_calling_Must_BeWithin
   {
      readonly DateTime _reference = DateTimeCE.ParseInvariant("2025-01-01 12:00:00");

      [XF] public void it_does_not_throw_when_exactly_at_reference()
         => _reference.Must().BeWithin(TimeSpan.FromMinutes(5)).After(_reference);

      [XF] public void it_does_not_throw_when_within_tolerance_after_reference()
         => DateTimeCE.ParseInvariant("2025-01-01 12:03:00").Must().BeWithin(TimeSpan.FromMinutes(5)).After(_reference);

      [XF] public void it_does_not_throw_when_exactly_at_tolerance_boundary()
         => DateTimeCE.ParseInvariant("2025-01-01 12:05:00").Must().BeWithin(TimeSpan.FromMinutes(5)).After(_reference);

      [XF] public void it_throws_when_too_far_after_reference()
         => Invoking(() => DateTimeCE.ParseInvariant("2025-01-01 12:05:01").Must().BeWithin(TimeSpan.FromMinutes(5)).After(_reference))
           .Must()
           .Throw<AssertionFailedException>();

      [XF] public void it_throws_when_before_reference()
         => Invoking(() => DateTimeCE.ParseInvariant("2025-01-01 11:59:59").Must().BeWithin(TimeSpan.FromMinutes(5)).After(_reference))
           .Must()
           .Throw<AssertionFailedException>();
   }
}
