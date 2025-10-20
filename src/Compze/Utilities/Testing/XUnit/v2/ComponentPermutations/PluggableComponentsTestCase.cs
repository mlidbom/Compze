using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class PluggableComponentsTestCase : XunitTestCase
{
   [Obsolete("Called by deserializer")]
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
   {
   }

   public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                   IMessageBus messageBus,
                                                   object[] constructorArguments,
                                                   ExceptionAggregator aggregator,
                                                   CancellationTokenSource cancellationTokenSource)
   {
      return await TestContext.RunTestInContextAsync(
                this,
                //Manually override this rather than passing [] to the base class as the testMethodArguments constructor argument, because otherwise discovery breaks in NCrunch and Resharper test runners.
                async () => await new XunitTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, [], messageBus, aggregator, cancellationTokenSource).RunAsync());
   }
}
