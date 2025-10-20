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
      Type[]? skipExceptions,
      string? skipReason,
      Type? skipType,
      string? skipUnless,
      string? skipWhen,
      Dictionary<string, HashSet<string>> traits,
      string? sourceFilePath,
      int? sourceLineNumber,
      int? timeout,
      ComponentsPermutation permutation)
      : base(testMethod,
             testCaseDisplayName,
             uniqueID,
             @explicit,
             skipExceptions: skipExceptions,
             skipReason: skipReason,
             skipType: skipType,
             skipUnless: skipUnless,
             skipWhen: skipWhen,
             traits: traits,
             sourceFilePath: sourceFilePath,
             sourceLineNumber: sourceLineNumber,
             timeout: timeout,
             testMethodArguments: [permutation.ToString()]) // Pass as string or test discovery in dotnet test breaks
   {}

   public async ValueTask<RunSummary> Run(
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

      return await ComponentContext.RunTestInContextAsync(
                //We may get called on a serialized instance, so saving this in a field is trickier than you might think.
                //Keeping in mind the environmental constraints under which some test runners run, like NCrunch, this is actually a good idea.
                //If you ever consider changing it, DO make sure to test it thoroughly in every common test runner, including a long session of
                //"Activate Endless Churn Mode" in NCrunch
                ComponentsPermutation.Parse((string)TestMethodArguments![0]!),
                async () => await XunitRunnerHelper.RunXunitTestCase(
                               testCaseWithoutArgs,
                               messageBus,
                               cancellationTokenSource,
                               aggregator,
                               explicitOption,
                               constructorArguments));
   }
}
