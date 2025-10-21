using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class PluggableComponentsTestCase : XunitTestCase
{
   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      IMessageSink diagnosticMessageSink,
      TestMethodDisplay defaultMethodDisplay,
      TestMethodDisplayOptions defaultMethodDisplayOptions,
      ITestMethod testMethod,
      string permutationString)
      : base(diagnosticMessageSink,
             defaultMethodDisplay,
             defaultMethodDisplayOptions,
             testMethod,
             [permutationString]) // Pass as string or test discovery in dotnet test breaks
   {}

   protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName) =>
      base.GetDisplayName(factAttribute, displayName).Replace("???:", ""); //the ???: is Xunit being confused because we have no arguments declare on the test methods

   public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                   IMessageBus messageBus,
                                                   object[] constructorArguments,
                                                   ExceptionAggregator aggregator,
                                                   CancellationTokenSource cancellationTokenSource)
   {
      return await TestContext.RunTestInContextAsync(
                this,
                async () => await ComponentsPermutation.RunInContextAsync(
                               //We may get called on a serialized instance, so saving this in a field is trickier than you might think.
                               //Keeping in mind the environmental constraints under which some test runners run, like NCrunch, this is actually a good idea.
                               //If you ever consider changing it, DO make sure to test it thoroughly in every common test runner, including a long session of
                               //"Activate Endless Churn Mode" in NCrunch
                               ComponentsPermutation.Parse((string)TestMethodArguments![0]!),
                               //Pass an empty arguments array since our test methods do not take arguments.
                               async () => await new XunitTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, [], messageBus, aggregator, cancellationTokenSource).RunAsync()));
   }
}
