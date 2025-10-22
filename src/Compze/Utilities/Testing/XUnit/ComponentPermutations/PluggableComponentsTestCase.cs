using Compze.Utilities.SystemCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class PluggableComponentsTestCase : ConstructorArgumentForwardingTestCase, ISelfExecutingXunitTestCase
{
   // Static storage for component types since test cases are serialized/deserialized
   static readonly Dictionary<string, Type[]> ComponentEnumTypesByTestMethod = new();

   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      XunitTestCase testCase,
      Dictionary<string, HashSet<string>> traits,
      Type[]? componentEnumTypes = null)
      : base(testCase,
             testCaseDisplayName: testCase.TestCaseDisplayName.Replace("???: ", "")) //the ???: is Xunit being confused because we have no arguments declare on the test methods.) // Pass as string or test discovery in dotnet test breaks
   {
      // Store component types in static dictionary using unique key
      if(componentEnumTypes != null && componentEnumTypes.Length > 0)
      {
         var key = $"{TestMethod.TestClass.Class.Name}.{TestMethod.Method.Name}";
         ComponentEnumTypesByTestMethod[key] = componentEnumTypes;
      }
   }

   public async ValueTask<RunSummary> Run(
      ExplicitOption explicitOption,
      IMessageBus messageBus,
      object?[] constructorArguments,
      ExceptionAggregator aggregator,
      CancellationTokenSource cancellationTokenSource)
   {
      // If there are no arguments or the test is skipped, just run it directly without setting up permutation context
      if(TestMethodArguments is null || TestMethodArguments.Length == 0 || !string.IsNullOrEmpty(SkipReason))
      {
         return await XunitRunnerHelper.RunXunitTestCase(
                   new ArgumentDiscardingTestCase(this),
                   messageBus,
                   cancellationTokenSource,
                   aggregator,
                   explicitOption,
                   constructorArguments);
      }

      return await ComponentsPermutation.RunInContextAsync(
                //We may get called on a serialized instance, so saving this in a field is trickier than you might think.
                //Keeping in mind the environmental constraints under which some test runners run, like NCrunch, this is actually a good idea.
                //If you ever consider changing it, DO make sure to test it thoroughly in every common test runner, including a long session of
                //"Activate Endless Churn Mode" in NCrunch
                //It is lazy because run is called even for ignored tests etc. So we cannot assume that we have arguments.
                new LazyCE<ComponentsPermutation>(() => {
                   var argString = (string)TestMethodArguments![0]!;
                   
                   // Retrieve component types from static storage
                   var key = $"{TestMethod.TestClass.Class.Name}.{TestMethod.Method.Name}";
                   ComponentEnumTypesByTestMethod.TryGetValue(key, out var componentEnumTypes);
                   
                   return ComponentsPermutation.Parse(argString, componentEnumTypes);
                }),
                async () => await XunitRunnerHelper.RunXunitTestCase(
                               new ArgumentDiscardingTestCase(this),
                               messageBus,
                               cancellationTokenSource,
                               aggregator,
                               explicitOption,
                               constructorArguments));
   }
}
