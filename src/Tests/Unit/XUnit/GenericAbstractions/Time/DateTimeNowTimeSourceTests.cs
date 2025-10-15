using System;
using Compze.Abstractions.Internal.Time;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Compze.Tests.Unit.XUnit.GenericAbstractions.Time;

public class DateTimeNowTimeSourceTests : XUnitTestBase
{
   [XFact] public void UtcNow_should_return_within_100_milliseconds_of_DateTimeNow()
   {
      var uut = new DateTimeNowTimeSource();
      uut.UtcNow.Should().BeWithin(100.Milliseconds()).Before(DateTime.UtcNow);
   }
}
