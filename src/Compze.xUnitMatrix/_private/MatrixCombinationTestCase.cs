using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Xunit.Sdk;
using Xunit.v3;
using Compze.xUnit._internal;

namespace Compze.xUnitMatrix._private;

class MatrixCombinationTestCase : ConstructorArgumentForwardingTestCase, ISelfExecutingXunitTestCase
{
   static readonly IReadOnlyList<string> HiddenArgumentNames = ["components", "???"]; //the ???: is Xunit being confused because we have no arguments declared on the test methods.

   static string ReplaceArgumentNames(string testMethodName) =>
      HiddenArgumentNames.Aggregate(testMethodName, (current, hidden) => current.ReplaceOrdinal($"{hidden}: ", ""));

   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public MatrixCombinationTestCase() {}

   public MatrixCombinationTestCase(XunitTestCase testCase)
      : base(testCase, testCaseDisplayName: ReplaceArgumentNames(testCase.TestCaseDisplayName))
   {}

   MatrixCombination Combination => (MatrixCombination)TestMethodArguments[0]!;

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
                               new ArgumentDiscardingTestCase(this),
                               messageBus,
                               cancellationTokenSource,
                               aggregator,
                               explicitOption,
                               constructorArguments).caf()).caf();
   }
}
