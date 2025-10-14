using System;
using Compze.Abstractions.Internal.Time;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Compze.Tests.Unit.XUnit.GenericAbstractions.Time;

public class DateTimeNowTimeSourceTests : XUnitTestBase
{
   [Fact] public void UtcNow_should_return_within_100_milliseconds_of_DateTimeNow()
   {
      var uut = new DateTimeNowTimeSource();
      uut.UtcNow.Should().BeWithin(100.Milliseconds()).Before(DateTime.UtcNow);
   }
}
