using System.Reflection;


namespace Compze.xUnitMatrix.Tests.Configuration;

// A configuration file that cannot be read does not crash discovery: the test is surfaced as a single skipped row whose
// reason explains what went wrong.
public class WhenTheConfigurationFileCannotBeUsed
{
   [Fact] public async Task A_missing_file_is_reported_in_the_skip_reason() =>
      (await SkipReasonFor(new MissingConfigurationFileMatrixAttribute())).Must().Contain("File does not exist");

   [Fact] public async Task An_unparseable_dimension_value_is_reported_in_the_skip_reason() =>
      (await SkipReasonFor(new InvalidDimensionValueMatrixAttribute())).Must().Contain("NotASqlLayer");

   [Fact] public async Task A_file_with_no_combinations_is_reported_in_the_skip_reason() =>
      (await SkipReasonFor(new EmptyConfigurationFileMatrixAttribute())).Must().Contain("found no configured combinations");

   static async Task<string> SkipReasonFor(MatrixTheoryAttribute matrix)
   {
      var rows = await MatrixDiscovery.DiscoverRows(
         matrix,
         typeof(WhenTheConfigurationFileCannotBeUsed).GetMethod(nameof(SkipReasonFor), BindingFlags.Static | BindingFlags.NonPublic)!);

      rows.Must().HaveCount(1);
      return rows.Single().Skip!;
   }
}
