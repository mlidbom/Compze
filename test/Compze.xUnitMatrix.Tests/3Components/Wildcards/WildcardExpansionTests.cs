using System.Reflection;


namespace Compze.xUnitMatrix.Tests._3Components.Wildcards;

public class WildcardExpansionTests
{
   static readonly string[] SingleWildcardExpansion =                            // TestUsingWildcards: Microsoft:*:Microsoft
   [
      "Microsoft:MsSql:Microsoft",
      "Microsoft:MySql:Microsoft",
      "Microsoft:Postgre:Microsoft"
   ];

   static readonly string[] MultipleWildcardCartesianProduct =                   // TestUsingMultipleWildcards: Microsoft:*:*
   [
      "Microsoft:MsSql:Microsoft", "Microsoft:MsSql:Autofac", "Microsoft:MsSql:DryIoc",
      "Microsoft:Postgre:Microsoft", "Microsoft:Postgre:Autofac", "Microsoft:Postgre:DryIoc",
      "Microsoft:MySql:Microsoft", "Microsoft:MySql:Autofac", "Microsoft:MySql:DryIoc"
   ];

   static readonly string[] DeduplicatedCombinations =                           // Microsoft:MsSql:Microsoft is produced by three lines.
   [
      "Microsoft:MsSql:Microsoft",
      "Microsoft:Postgre:Microsoft",
      "Microsoft:MySql:Microsoft",
      "Newtonsoft:MsSql:Microsoft"
   ];

   [Fact] public async Task A_wildcard_expands_to_every_value_of_its_dimension() =>
      (await ExpandedCombinationsOf(new WildcardTestAttribute()))
         .Order().Must().SequenceEqual(SingleWildcardExpansion.Order());

   [Fact] public async Task Multiple_wildcards_in_one_line_expand_to_their_cartesian_product() =>
      (await ExpandedCombinationsOf(new MultipleWildcardsTestAttribute()))
         .Order().Must().SequenceEqual(MultipleWildcardCartesianProduct.Order());

   [Fact] public async Task Combinations_produced_by_more_than_one_line_are_deduplicated() =>
      (await ExpandedCombinationsOf(new WildcardDeduplicationTestAttribute()))
         .Order().Must().SequenceEqual(DeduplicatedCombinations.Order());

   static async Task<IReadOnlyList<string>> ExpandedCombinationsOf(MatrixTheoryAttribute matrix) =>
      (await MatrixDiscovery.DiscoverRows(
          matrix,
          typeof(WildcardExpansionTests).GetMethod(nameof(ExpandedCombinationsOf), BindingFlags.Static | BindingFlags.NonPublic)!))
     .CombinationNames();
}
