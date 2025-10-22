using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents.NoArgumentPassing;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [NoArgumentPassingTwoComponentsPCT]
   public void AnExceptionIsThrown()
   {
      FluentActions.Invoking(() => new NoArgumentPassingTwoComponentsPCTAttribute(skipped: ["invalid"], skipReasons: ["because something"]))
                   .Should().Throw<Exception>();
   }
}
