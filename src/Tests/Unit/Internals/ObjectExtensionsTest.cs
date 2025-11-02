using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Functional;
using Compze.Utilities.Testing.XUnit.BDD;
using Compze.Tests.Infrastructure.Fluent;

namespace Compze.Tests.Unit.Internals;


public class ObjectExtensionsTest : UniversalTestBase
{
   [XF]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => 12.Repeat(10).Count().Must().Be(10);
}
