using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.ComponentPermutations;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [PCT] public void AnExceptionIsThrownWhenAcessingPermutations()
   {
      var pctAttribute = new PCTAttribute()
                         {
                            Skipped = ["Invalid::NotSupported"]
                         };

#pragma warning disable CS0618 // Type or member is obsolete
      pctAttribute.Invoking(it => it.GetTheoryDataRowsInternal()).Should().Throw<Exception>();
#pragma warning restore CS0618 // Type or member is obsolete
   }
}
