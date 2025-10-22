namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [OurPCT]
   public void TheCodeDoesNotCompile()
   {
      var attribute = new OurPCTAttribute(skipped: ["invalid"]);
      var values = attribute.GetTheoryDataRowsInternal();
   }
}
