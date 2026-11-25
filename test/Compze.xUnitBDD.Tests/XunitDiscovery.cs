using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitBDD.Tests;

///<summary>
/// Drives xUnit's discovery of a class's facts directly, as a test runner's discovery does, so that specifications can assert<br/>
/// which test cases it produces, and run them: also for classes that xUnit's own discovery skips.
///</summary>
static class XunitDiscovery
{
   /// <summary>
   /// The test cases xUnit discovers in <paramref name="testClass"/> for its fact <paramref name="testMethodName"/>, declared<br/>
   /// there or inherited: what a test runner lists and runs for it in that class.
   /// </summary>
   public static Task<IReadOnlyCollection<IXunitTestCase>> TestCases(Type testClass, string testMethodName) =>
      TestCases(TestClassOf(testClass), testClass.GetMethod(testMethodName)!);

   /// <summary>The test cases xUnit discovers in <paramref name="testClass"/> for each of its facts, declared there or inherited.</summary>
   public static async Task<IReadOnlyCollection<IXunitTestCase>> TestCases(IXunitTestClass testClass)
   {
      var testCases = new List<IXunitTestCase>();
      foreach(var testMethod in testClass.Class.GetMethods().Where(IsAFact))
         testCases.AddRange(await TestCases(testClass, testMethod));
      return testCases;
   }

   /// <summary><paramref name="testClass"/>, as xUnit's discovery sees it: in a test collection of its own.</summary>
   public static XunitTestClass TestClassOf(Type testClass) =>
      new(testClass,
          new XunitTestCollection(new XunitTestAssembly(testClass.Assembly, configFilePath: null),
                                  collectionDefinition: null,
                                  disableParallelization: false,
                                  displayName: testClass.Name));

   static async Task<IReadOnlyCollection<IXunitTestCase>> TestCases(IXunitTestClass testClass, MethodInfo testMethod)
   {
      var fact = testMethod.GetCustomAttributes(inherit: true).OfType<IFactAttribute>().Single();
      var discovererType = fact.GetType().GetCustomAttributes(inherit: true).OfType<IXunitTestCaseDiscovererAttribute>().Single().Type;
      var discoverer = (IXunitTestCaseDiscoverer)Activator.CreateInstance(discovererType)!;

      return await discoverer.Discover(new DefaultDiscoveryOptions(), new XunitTestMethod(testClass, testMethod, testMethodArguments: []), fact);
   }

   static bool IsAFact(MethodInfo method) => method.GetCustomAttributes(inherit: true).OfType<IFactAttribute>().Any();
}
