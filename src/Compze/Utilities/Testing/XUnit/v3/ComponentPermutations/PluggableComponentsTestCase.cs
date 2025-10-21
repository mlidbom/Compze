using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class PluggableComponentsTestCase : XunitTestCase, ISelfExecutingXunitTestCase
{
   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(XunitTestCase testCase)
      : base(testMethod: testCase.TestMethod,
             testCaseDisplayName: testCase.TestCaseDisplayName,
             uniqueID: testCase.UniqueID,
             @explicit: testCase.Explicit,
             skipExceptions: testCase.SkipExceptions,
             skipReason: testCase.SkipReason,
             skipType: testCase.SkipType,
             skipUnless: testCase.SkipUnless,
             skipWhen: testCase.SkipWhen,
             traits: testCase.Traits,
             sourceFilePath: testCase.SourceFilePath,
             sourceLineNumber: testCase.SourceLineNumber,
             timeout: testCase.Timeout,
             testMethodArguments: testCase.TestMethodArguments) // Pass as string or test discovery in dotnet test breaks
   {}

   public async ValueTask<RunSummary> Run(
      ExplicitOption explicitOption,
      IMessageBus messageBus,
      object?[] constructorArguments,
      ExceptionAggregator aggregator,
      CancellationTokenSource cancellationTokenSource)
   {
      // Create a new test case with empty test method arguments to execute our test methods that do not take arguments
      var testCaseWithoutArgs = new ArgumentDiscardingTestCase(this);

      return await ComponentsPermutation.RunInContextAsync(
                //We may get called on a serialized instance, so saving this in a field is trickier than you might think.
                //Keeping in mind the environmental constraints under which some test runners run, like NCrunch, this is actually a good idea.
                //If you ever consider changing it, DO make sure to test it thoroughly in every common test runner, including a long session of
                //"Activate Endless Churn Mode" in NCrunch
                ComponentsPermutation.Parse((string)TestMethodArguments![0]!),
                async () => await XunitRunnerHelper.RunXunitTestCase(testCaseWithoutArgs,
                                                                     messageBus,
                                                                     cancellationTokenSource,
                                                                     aggregator,
                                                                     explicitOption,
                                                                     constructorArguments));
   }
}
