using System;
using System.Globalization;
using Compze.Core.Time.Public;
using Compze.Core.Time.Testing.Public;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;

namespace Compze.Tests.Unit.GenericAbstractions.Time;

public class DummyTimeSourceTests : UniversalTestBase
{
   [XF]
   public void Now_should_return_within_100_millisecond_of_datetime_UtcNow()
   {
      TestingTimeSourceStatic.FrozenAtUtcNow().Run(() =>
      {
         Math.Abs((UtcTimeSource.UtcNow - DateTime.UtcNow).TotalMilliseconds).Should().BeLessThan(100);
      });

   }

   [XF]
   public void FromUtcTime_returns_an_instance_with_UtcTime_equal_to_supplied_value()
   {
      var utcNow = DateTime.UtcNow;
      TestingTimeSourceStatic.FrozenAtUtc(utcNow).Run(() =>
      {
         UtcTimeSource.UtcNow.Should().Be(utcNow);
      });
   }

   [XF]
   public void Passing_a_parsed_date_to_FromUtcTime_results_in_UtcNow_being_that_date()
   {
      var dateTime = DateTime.Parse("2001-01-01 00:00", CultureInfo.InvariantCulture).ToUniversalTime();
      TestingTimeSourceStatic.FrozenAtUtc("2001-01-01 00:00").Run(() =>
      {
         UtcTimeSource.UtcNow.Should().Be(dateTime);
      });
   }
}
