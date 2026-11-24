using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitBDD.Tests;

///<summary>
/// Drives an [XF]'s xUnit discovery directly, so that specifications can assert which test cases it produces for a class,
/// classes that xUnit's own discovery skips included.
///</summary>
static class ExclusiveFactDiscovery
{
   /// <summary>
   /// The test cases xUnit discovers in <paramref name="testClass"/> for its [XF] <paramref name="testMethodName"/>, declared
   /// there or inherited: what a test runner lists and runs for it in that class.
   /// </summary>
   public static async Task<IReadOnlyCollection<IXunitTestCase>> TestCases(Type testClass, string testMethodName)
   {
      var testMethod = testClass.GetMethod(testMethodName)!;
      var exclusiveFact = testMethod.GetCustomAttributes(typeof(ExclusiveFactAttribute), inherit: true).Cast<ExclusiveFactAttribute>().Single();
      var discovererType = exclusiveFact.GetType().GetCustomAttributes(inherit: true).OfType<IXunitTestCaseDiscovererAttribute>().Single().Type;
      var discoverer = (IXunitTestCaseDiscoverer)Activator.CreateInstance(discovererType)!;

      var testCollection = new XunitTestCollection(new XunitTestAssembly(testClass.Assembly, configFilePath: null),
                                                   collectionDefinition: null,
                                                   disableParallelization: false,
                                                   displayName: nameof(ExclusiveFactDiscovery));
      var xunitTestMethod = new XunitTestMethod(new XunitTestClass(testClass, testCollection), testMethod, testMethodArguments: []);

      return await discoverer.Discover(new DefaultDiscoveryOptions(), xunitTestMethod, exclusiveFact);
   }
}
