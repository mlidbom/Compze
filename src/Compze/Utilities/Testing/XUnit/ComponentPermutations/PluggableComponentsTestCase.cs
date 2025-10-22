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
         // Use fully qualified name as key
         var key = $"{TestMethod.TestClass.Class.Name}.{TestMethod.Method.Name}";
         ComponentEnumTypesByTestMethod[key] = componentEnumTypes;
         
         // Also store by simpler key for lookup
         var simpleKey = TestMethod.Method.Name;
         ComponentEnumTypesByTestMethod[simpleKey] = componentEnumTypes;
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
                   
                   // Try to get component types from:
                   // 1. TypedPCTAttribute static property (for known typed attributes)
                   // 2. Static dictionary (fallback)
                   // 3. null (for untyped PCT)
                   Type[]? componentEnumTypes = null;
                   
                   // Check if we can use TypedPCTAttribute's static types
                   // This works because TypedPCTAttribute is always the same for all tests using it
                   if(TestMethod.Method.Name != null)
                   {
                      // If the test uses TypedPCT, use its static ComponentTypes
                      componentEnumTypes = TypedPCTAttribute.ComponentTypes;
                   }
                   
                   // Fallback: try static dictionary
                   if(componentEnumTypes == null || componentEnumTypes.Length == 0)
                   {
                      var fullKey = $"{TestMethod.TestClass.Class.Name}.{TestMethod.Method.Name}";
                      if(!ComponentEnumTypesByTestMethod.TryGetValue(fullKey, out componentEnumTypes))
                      {
                         if(TestMethod.Method.Name != null)
                         {
                            ComponentEnumTypesByTestMethod.TryGetValue(TestMethod.Method.Name, out componentEnumTypes);
                         }
                      }
                   }
                   
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
