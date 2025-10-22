using Compze.Utilities.SystemCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class PluggableComponentsTestCase : ConstructorArgumentForwardingTestCase, ISelfExecutingXunitTestCase
{
   Type[] _componentEnumTypes;

   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public PluggableComponentsTestCase() => _componentEnumTypes = [];

   public PluggableComponentsTestCase(
      XunitTestCase testCase,
      Dictionary<string, HashSet<string>> traits,
      Type[] componentEnumTypes)
      : base(testCase,
             testCaseDisplayName: testCase.TestCaseDisplayName.Replace("???: ", "") //the ???: is Xunit being confused because we have no arguments declare on the test methods.
      ) => _componentEnumTypes = componentEnumTypes;

   protected override void Serialize(IXunitSerializationInfo info)
   {
      base.Serialize(info);
      info.AddValue(nameof(_componentEnumTypes), _componentEnumTypes.Select(it => it.AssemblyQualifiedName!).ToArray());
   }

   protected override void Deserialize(IXunitSerializationInfo info)
   {
      base.Deserialize(info);
      var enumTypeString = info.GetValue<string[]>(nameof(_componentEnumTypes)) ?? throw new Exception($"{nameof(_componentEnumTypes)} not found in the serialized data.");
      _componentEnumTypes = enumTypeString.Select(it => Type.GetType(it, throwOnError: true)!).ToArray();
   }

   public async ValueTask<RunSummary> Run(
      ExplicitOption explicitOption,
      IMessageBus messageBus,
      object?[] constructorArguments,
      ExceptionAggregator aggregator,
      CancellationTokenSource cancellationTokenSource)
   {
      return await ComponentsPermutation.RunInContextAsync(
                //We may get called on a serialized instance, so saving this in a field is trickier than you might think.
                //Keeping in mind the environmental constraints under which some test runners run, like NCrunch,
                //If you ever consider changing it, DO make sure to test it thoroughly in every common test runner, including a long session of
                //"Activate Endless Churn Mode" in NCrunch
                //It is lazy because run is called even for ignored tests etc. So we cannot assume that we have arguments.
                new LazyCE<ComponentsPermutation>(() => ComponentsPermutation.Parse((string)TestMethodArguments![0]!,
                                                                                    _componentEnumTypes)),
                async () => await XunitRunnerHelper.RunXunitTestCase(
                               new ArgumentDiscardingTestCase(this),
                               messageBus,
                               cancellationTokenSource,
                               aggregator,
                               explicitOption,
                               constructorArguments));
   }
}
