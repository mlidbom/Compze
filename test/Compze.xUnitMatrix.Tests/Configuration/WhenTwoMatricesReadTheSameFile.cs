using System.Reflection;

namespace Compze.xUnitMatrix.Tests.Configuration;

// A file is read for each set of dimensions it is read for: a matrix never gets combinations read for another matrix's dimensions.
public class WhenTwoMatricesReadTheSameFile
{
   [Fact] public async Task each_matrix_gets_the_combinations_of_its_own_dimensions()
   {
      (await CombinationsOf(new SerializerAndSqlLayerFromASharedFileMatrixAttribute()))
        .Order().Must().SequenceEqual(["Microsoft:MsSql", "Microsoft:MySql", "Microsoft:Postgre"]);

      (await CombinationsOf(new SerializerAndDIContainerFromASharedFileMatrixAttribute()))
        .Order().Must().SequenceEqual(["Microsoft:Autofac", "Microsoft:DryIoc", "Microsoft:Microsoft"]);
   }

   static async Task<IReadOnlyList<string>> CombinationsOf(MatrixTheoryAttribute matrix) =>
      (await MatrixDiscovery.DiscoverRows(
          matrix,
          typeof(WhenTwoMatricesReadTheSameFile).GetMethod(nameof(CombinationsOf), BindingFlags.Static | BindingFlags.NonPublic)!))
     .CombinationNames();
}
