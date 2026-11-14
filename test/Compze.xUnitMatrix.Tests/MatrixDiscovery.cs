using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitMatrix.Tests;

///<summary>
/// Drives a matrix attribute's xUnit discovery directly so specs can assert exactly which combinations, or which test cases,
/// it produces, without having to run a test once per combination.
///</summary>
static class MatrixDiscovery
{
   public static async Task<IReadOnlyCollection<ITheoryDataRow>> DiscoverRows(IDataAttribute matrixAttribute, MethodInfo testMethod)
   {
      await using var disposalTracker = new DisposalTracker();
      return await matrixAttribute.GetData(testMethod, disposalTracker);
   }

   public static IReadOnlyList<string> CombinationNames(this IReadOnlyCollection<ITheoryDataRow> rows) =>
      rows.Where(row => row.Skip == null)
          .Select(row => ((MatrixCombination)row.GetData()[0]!).ToString())
          .ToList();

   /// <summary>
   /// The test cases xUnit discovers for the matrix test <paramref name="testMethodName"/> of <paramref name="testClass"/>: what
   /// a test runner lists and runs for it. Discovers them as xUnit does, with the discoverer the test's matrix attribute names.
   /// </summary>
   /// <remarks>
   /// The test class may be abstract: xUnit's own discovery skips abstract classes, which lets a specification keep matrix
   /// tests that must not run, such as ones whose configuration cannot be used, and discover them only here.
   /// </remarks>
   public static async Task<IReadOnlyCollection<IXunitTestCase>> DiscoverTestCases(Type testClass, string testMethodName)
   {
      var testMethod = testClass.GetMethod(testMethodName)!;
      var matrixAttribute = testMethod.GetCustomAttributes().OfType<MatrixTheoryAttribute>().Single();
      var discovererType = matrixAttribute.GetType().GetCustomAttributes(inherit: true).OfType<IXunitTestCaseDiscovererAttribute>().Single().Type;
      var discoverer = (IXunitTestCaseDiscoverer)Activator.CreateInstance(discovererType)!;

      var testCollection = new XunitTestCollection(new XunitTestAssembly(testClass.Assembly, configFilePath: null),
                                                   collectionDefinition: null,
                                                   disableParallelization: false,
                                                   displayName: nameof(MatrixDiscovery));
      var xunitTestMethod = new XunitTestMethod(new XunitTestClass(testClass, testCollection), testMethod, testMethodArguments: []);

      return await discoverer.Discover(new DefaultDiscoveryOptions(), xunitTestMethod, matrixAttribute);
   }
}
