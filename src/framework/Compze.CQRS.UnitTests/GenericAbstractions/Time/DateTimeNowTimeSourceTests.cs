using System;
using Composable.GenericAbstractions.Time;
using Composable.SystemCE;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.GenericAbstractions.Time;

[TestFixture]
public class DateTimeNowTimeSourceTests : UniversalTestBase
{
   [Test]
   public void UtcNow_should_return_within_100_milliseconds_of_DateTimeNow()
   {
      var uut = new DateTimeNowTimeSource();
      uut.UtcNow.Should().BeWithin(100.Milliseconds()).Before(DateTime.UtcNow);
   }
}