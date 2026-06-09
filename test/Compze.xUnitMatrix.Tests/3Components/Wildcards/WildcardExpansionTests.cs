namespace Compze.xUnitMatrix.Tests._3Components.Wildcards;

public class WildcardExpansionTests(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [WildcardTest]
   public void WildcardsExpandCorrectly()
   {
      var combination = MatrixCombination.Current;
      _testOutputHelper.WriteLine($"combination: {combination}");

      // TestUsingWildcards contains a single line: Microsoft:*:Microsoft
      // The * in the SqlLayer position expands to every SqlLayer value, so this test runs
      // once per SqlLayer with Serializer and DIContainer fixed to Microsoft:
      //   Microsoft:MsSql:Microsoft, Microsoft:Postgre:Microsoft, Microsoft:MySql:Microsoft
   }
}
