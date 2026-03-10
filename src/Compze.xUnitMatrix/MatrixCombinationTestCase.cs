using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE;
using Compze.xUnit;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitMatrix;

class MatrixCombinationTestCase : ConstructorArgumentForwardingTestCase, ISelfExecutingXunitTestCase
{
   bool _useTestMethodArguments;

   static readonly IReadOnlyList<string> HiddenArgumentNames = ["combination", "components", "???"]; //the ???: is Xunit being confused because we have no arguments declare on the test methods.

   static string ReplaceArgumentNames(string testMethodName) =>
      HiddenArgumentNames.Aggregate(testMethodName, (current, hidden) => current.ReplaceOrdinal($"{hidden}: ", ""));

   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public MatrixCombinationTestCase() {}

   // ReSharper disable once ConvertToPrimaryConstructor
   public MatrixCombinationTestCase(XunitTestCase testCase, bool useTestMethodArguments)
      : base(testCase, testCaseDisplayName: ReplaceArgumentNames(testCase.TestCaseDisplayName)) =>
      _useTestMethodArguments = useTestMethodArguments;

   MatrixCombination Combination => (MatrixCombination)TestMethodArguments[0]!;

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
      return await MatrixCombination.RunInContextAsync(
                new LazyCE<MatrixCombination>(() => Combination),
                async () => await XunitRunnerHelper.RunXunitTestCase(
                               _useTestMethodArguments ? this : new ArgumentDiscardingTestCase(this),
                               messageBus,
                               cancellationTokenSource,
                               aggregator,
                               explicitOption,
                               constructorArguments).caf()).caf();
   }
}
