using System;
using Compze.Abstractions.Internal.Time;
using Compze.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Unit.GenericAbstractions.Time;

[TestFixture] public class DateTimeNowTimeSourceTests : UniversalTestBase
{
   [Test] public void UtcNow_should_return_within_100_milliseconds_of_DateTimeNow()
   {
      var uut = new DateTimeNowTimeSource();
      uut.UtcNow.Should().BeWithin(100.Milliseconds()).Before(DateTime.UtcNow);
   }
}
