using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [TypedPCT] public void AnExceptionIsThrownWhenAcessingPermutations()
   {
      // This test validates that TypedPCT throws when given an invalid component
      // We use the string-based Skipped property here since we can't pass invalid enum values at compile time
      var pctAttribute = new PCTAttribute()
                         {
                            Skipped = ["Invalid::NotSupported"]
                         };

#pragma warning disable CS0618 // Type or member is obsolete
      pctAttribute.Invoking(it => it.GetTheoryDataRowsInternal()).Should().Throw<Exception>();
#pragma warning restore CS0618 // Type or member is obsolete
   }
}
