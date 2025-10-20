using System;
using Compze.Abstractions.Internal.Time;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Compze.Tests.Unit.GenericAbstractions.Time;

public class DateTimeNowTimeSourceTests : UniversalTestBase
{
   [XF] public void UtcNow_should_return_within_100_milliseconds_of_DateTimeNow()
   {
      var uut = new DateTimeNowTimeSource();
      uut.UtcNow.Should().BeWithin(100.Milliseconds()).Before(DateTime.UtcNow);
   }
}
