using Compze.Core.Time.Public;
using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.GenericAbstractions.Time;

public class DateTimeNowTimeSourceTests : UniversalTestBase
{
   [XF] public void UtcNow_should_return_within_100_milliseconds_of_DateTimeNow()
   {
      var uut = new DateTimeNowTimeSource();
      uut.UtcNow.Must().Be(DateTime.UtcNow, TimeSpan.FromMilliseconds(100));
   }
}
