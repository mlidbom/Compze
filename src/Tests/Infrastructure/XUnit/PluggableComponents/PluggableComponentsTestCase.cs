using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

public class PluggableComponentsTestCase : XunitTestCase
{
   Tessaging.Hosting.Testing.PluggableComponents? _combination = null;

   public Tessaging.Hosting.Testing.PluggableComponents Components => _combination!.Value;

   [Obsolete("Called by deserializer")]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      IMessageSink diagnosticMessageSink,
      TestMethodDisplay defaultMethodDisplay,
      TestMethodDisplayOptions defaultMethodDisplayOptions,
      ITestMethod testMethod,
      Tessaging.Hosting.Testing.PluggableComponents combination,
      object[]? testMethodArguments = null)
      : base(diagnosticMessageSink,
             defaultMethodDisplay,
             defaultMethodDisplayOptions,
             testMethod,
             testMethodArguments)
   {
      _combination = combination;
      DisplayName = $"{testMethod.Method.Name}({combination})";
   }

   public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                   IMessageBus messageBus,
                                                   object[] constructorArguments,
                                                   ExceptionAggregator aggregator,
                                                   CancellationTokenSource cancellationTokenSource)
   {
      // Set the current combination in AsyncLocal so TestEnv can access it
      XUnitInfrastructureModuleInitializer.CurrentPluggableComponents.Value = _combination;
      try
      {
         return await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
      }
      finally
      {
         XUnitInfrastructureModuleInitializer.CurrentPluggableComponents.Value = null;
      }
   }

   public override void Serialize(IXunitSerializationInfo data)
   {
      base.Serialize(data);
      data.AddValue(nameof(_combination), _combination.ToString());
   }

   public override void Deserialize(IXunitSerializationInfo data)
   {
      base.Deserialize(data);
      _combination = Tessaging.Hosting.Testing.PluggableComponents.FromString(data.GetValue<string>(nameof(_combination)).NotNull());
   }
}
