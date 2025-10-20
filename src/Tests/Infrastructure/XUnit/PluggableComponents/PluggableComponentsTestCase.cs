using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

class PluggableComponentsTestCase : XunitTestCase
{
   [Obsolete("Called by deserializer")]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      IMessageSink diagnosticMessageSink,
      TestMethodDisplay defaultMethodDisplay,
      TestMethodDisplayOptions defaultMethodDisplayOptions,
      ITestMethod testMethod,
      Tessaging.Hosting.Testing.PluggableComponents combination)
      : base(diagnosticMessageSink,
             defaultMethodDisplay,
             defaultMethodDisplayOptions,
             testMethod,
             [combination.ToString()]) // Pass as string or test discovery in dotnet test breaks
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
