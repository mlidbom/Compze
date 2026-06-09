using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitMatrix.Tests;

///<summary>
/// Drives a matrix attribute's xUnit discovery directly so specs can assert exactly which combinations — or which skip
/// reason — it produces, without having to run a test once per combination.
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
}
