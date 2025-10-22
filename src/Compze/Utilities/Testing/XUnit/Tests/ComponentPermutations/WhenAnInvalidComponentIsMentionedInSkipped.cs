using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [OurPCT]
   public void AnExceptionIsThrown()
   {
      FluentActions.Invoking(() => new OurPCTAttribute(skipped: ["invalid"], skipReasons: ["because something"]))
                   .Should().Throw<Exception>();
   }
}
