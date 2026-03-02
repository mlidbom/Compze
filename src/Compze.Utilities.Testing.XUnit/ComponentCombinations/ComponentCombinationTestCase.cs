using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

class ComponentCombinationTestCase : ConstructorArgumentForwardingTestCase, ISelfExecutingXunitTestCase
{
   bool _useTestMethodArguments;

   static readonly IReadOnlyList<string> HiddenArgumentNames = ["combination", "components", "???"]; //the ???: is Xunit being confused because we have no arguments declare on the test methods.

   static string ReplaceArgumentNames(string testMethodName) =>
      HiddenArgumentNames.Aggregate(testMethodName, (current, hidden) => current.ReplaceCE($"{hidden}: ", ""));

   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public ComponentCombinationTestCase() {}

   public ComponentCombinationTestCase(
      XunitTestCase testCase,
      bool useTestMethodArguments,
      Dictionary<string, HashSet<string>> traits)
      : base(testCase,
             testCaseDisplayName: ReplaceArgumentNames(testCase.TestCaseDisplayName)
      ) =>
      _useTestMethodArguments = useTestMethodArguments;

   ComponentCombination combination => (ComponentCombination)TestMethodArguments[0]!;

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
      return await ComponentCombination.RunInContextAsync(
                new LazyCE<ComponentCombination>(() => combination),
                async () => await XunitRunnerHelper.RunXunitTestCase(
                               _useTestMethodArguments ? this : new ArgumentDiscardingTestCase(this),
                               messageBus,
                               cancellationTokenSource,
                               aggregator,
                               explicitOption,
                               constructorArguments).caf()).caf();
   }
}
