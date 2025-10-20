using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class PluggableComponentsTestCase : XunitTestCase, ISelfExecutingXunitTestCase
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
   {}

   public ValueTask<RunSummary> Run(
      ExplicitOption explicitOption,
      IMessageBus messageBus,
      object?[] constructorArguments,
      ExceptionAggregator aggregator,
      CancellationTokenSource cancellationTokenSource)
   {
      //Manually override testMethodArguments to [] rather than passing [] to the base class as the testMethodArguments constructor argument, because otherwise discovery breaks in NCrunch and Resharper test runners.
      // Create a new test case with empty test method arguments
      var testCaseWithoutArgs = new XunitTestCase(
         TestMethod,
         TestCaseDisplayName,
         UniqueID,
         Explicit,
         skipReason: SkipReason,
         skipType: SkipType,
         skipUnless: SkipUnless,
         skipWhen: SkipWhen,
         traits: Traits,
         timeout: Timeout,
         testMethodArguments: []);

      return XunitRunnerHelper.RunXunitTestCase(
         testCaseWithoutArgs,
         messageBus,
         cancellationTokenSource,
         aggregator,
         explicitOption,
         constructorArguments);
   }
}
