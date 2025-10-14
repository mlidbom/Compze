using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

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
         new TestContextData(null, TestMethod),
         () => base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource));
   }
}
