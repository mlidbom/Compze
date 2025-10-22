namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [OurPCT]
   public void TheCodeDoesNotCompile()
   {
      var attribute = new OurPCTAttribute(skipped: ["invalid"]);
#pragma warning disable CS0618 // Type or member is obsolete
      var values = attribute.GetTheoryDataRowsInternal();
#pragma warning restore CS0618 // Type or member is obsolete
   }
}
