using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit;


public class ObjectExtensionsTest : UniversalTestBase
{
   [XF]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => 12.Repeat(10).Count().Must().Be(10);
}
