using System;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components.NotArgumentPassing;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [NotArgumentPassingTwoComponentsPCT]
   public void AnExceptionIsThrown()
   {
      FluentActions.Invoking(() => new NotArgumentPassingTwoComponentsPCTAttribute(skipped: ["invalid"], skipReasons: ["because something"]))
                   .Should().Throw<Exception>();
   }
}
