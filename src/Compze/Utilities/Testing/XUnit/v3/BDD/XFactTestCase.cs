using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.BDD;

public class XFactTestCase : XunitTestCase
{
   [Obsolete("Called by deserializer")]
   public XFactTestCase() {}

   public XFactTestCase(
      IXunitTestMethod testMethod,
      string testCaseDisplayName,
      string uniqueID,
      bool @explicit,
      Dictionary<string, HashSet<string>> traits)
      : base(testMethod,
             testCaseDisplayName,
             uniqueID,
             @explicit,
             traits: traits)
   {
   }
}
