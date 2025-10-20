using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Utilities.Testing.XUnit.BDD;

public class XFactTestCase : XunitTestCase
{
   [Obsolete("Called by deserializer")]
   public XFactTestCase() {}

   public XFactTestCase(
      IMessageSink diagnosticMessageSink,
      TestMethodDisplay defaultMethodDisplay,
      TestMethodDisplayOptions defaultMethodDisplayOptions,
      ITestMethod testMethod,
      object[]? testMethodArguments = null)
      : base(diagnosticMessageSink,
             defaultMethodDisplay,
             defaultMethodDisplayOptions,
             testMethod,
             testMethodArguments)
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
         async () => await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource));
   }
}
