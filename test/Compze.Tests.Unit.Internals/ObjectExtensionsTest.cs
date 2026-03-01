using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Underscore;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.Internals;


public class ObjectExtensionsTest : UniversalTestBase
{
   [XF]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => 12.Repeat(10).Count().Must().Be(10);
}
