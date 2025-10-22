using Compze.Utilities.SystemCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class PluggableComponentsTestCase : ConstructorArgumentForwardingTestCase, ISelfExecutingXunitTestCase
{
   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      XunitTestCase testCase,
      Dictionary<string, HashSet<string>> traits)
      : base(testCase,
             testCaseDisplayName: testCase.TestCaseDisplayName.Replace("???: ", "") //the ???: is Xunit being confused because we have no arguments declare on the test methods.
      ) {}

   ComponentsPermutation Permutation => (ComponentsPermutation)TestMethodArguments![0]!;

   public async ValueTask<RunSummary> Run(
      ExplicitOption explicitOption,
      IMessageBus messageBus,
      object?[] constructorArguments,
      ExceptionAggregator aggregator,
      CancellationTokenSource cancellationTokenSource)
   {
      return await ComponentsPermutation.RunInContextAsync(
                new LazyCE<ComponentsPermutation>(() => Permutation),
                async () => await XunitRunnerHelper.RunXunitTestCase(
                               new ArgumentDiscardingTestCase(this),
                               messageBus,
                               cancellationTokenSource,
                               aggregator,
                               explicitOption,
                               constructorArguments));
   }
}
