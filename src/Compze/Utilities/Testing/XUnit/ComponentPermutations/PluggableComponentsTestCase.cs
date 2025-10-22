using Compze.Utilities.SystemCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class PluggableComponentsTestCase : ConstructorArgumentForwardingTestCase, ISelfExecutingXunitTestCase
{
   bool _useTestMethodArguments;

   static readonly IReadOnlyList<string> HiddenArgumentNames = ["permutation", "components", "???"]; //the ???: is Xunit being confused because we have no arguments declare on the test methods.

   static string ReplaceArgumentNames(string testMethodName) =>
      HiddenArgumentNames.Aggregate(testMethodName, (current, hidden) => current.Replace($"{hidden}: ", ""));

   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      XunitTestCase testCase,
      bool useTestMethodArguments,
      Dictionary<string, HashSet<string>> traits)
      : base(testCase,
             testCaseDisplayName: ReplaceArgumentNames(testCase.TestCaseDisplayName)
      ) =>
      _useTestMethodArguments = useTestMethodArguments;

   ComponentsPermutation Permutation => (ComponentsPermutation)TestMethodArguments![0]!;

   protected override void Serialize(IXunitSerializationInfo info)
   {
      base.Serialize(info);
      info.AddValue(nameof(_useTestMethodArguments), _useTestMethodArguments);
   }

   protected override void Deserialize(IXunitSerializationInfo info)
   {
      base.Deserialize(info);
      _useTestMethodArguments = info.GetValue<bool>(nameof(_useTestMethodArguments));
   }

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
                               _useTestMethodArguments ? this : new ArgumentDiscardingTestCase(this),
                               messageBus,
                               cancellationTokenSource,
                               aggregator,
                               explicitOption,
                               constructorArguments));
   }
}
