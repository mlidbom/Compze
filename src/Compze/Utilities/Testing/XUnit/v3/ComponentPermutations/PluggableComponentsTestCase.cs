using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class PluggableComponentsTestCase : XunitTestCase
{
   [Obsolete("Called by deserializer")]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      IXunitTestMethod testMethod,
      string testCaseDisplayName,
      string uniqueID,
      bool @explicit,
      Dictionary<string, HashSet<string>> traits,
      string permutationString)
      : base(testMethod,
             testCaseDisplayName,
             uniqueID,
             @explicit,
             traits: traits,
             testMethodArguments: [permutationString]) // Pass as string or test discovery in dotnet test breaks
   {
   }
}
