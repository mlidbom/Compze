using Xunit.v3;

namespace Compze.xUnitMatrix.Tests.Configuration;

// A matrix test whose configuration cannot be used is discovered as a single failing test, whose message says what is wrong.
// It used to be skipped, or to run with combinations that silently differed from the file, and a passing test run hid both.
public class WhenTheConfigurationCannotBeUsed
{
   [Fact] public async Task A_file_that_does_not_exist_fails_the_test_naming_the_file() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.ConfiguredByAFileThatDoesNotExist)))
        .Must().Contain("The matrix configuration file TestUsingAMissingConfigurationFile does not exist");

   [Fact] public async Task A_file_with_no_combinations_fails_the_test() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.ConfiguredByAFileWithNoCombinations)))
        .Must().Be("The matrix configuration file TestUsingNoCombinations lists no combinations: every line in it is blank or a comment.");

   [Fact] public async Task A_line_with_a_value_too_many_fails_the_test_naming_the_line() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.ConfiguredByALineWithAValueTooMany)))
        .Must().Be("Line 2 of the matrix configuration file TestUsingAValueTooMany, \"Microsoft:MsSql:Microsoft\", has 3 values, but the matrix has 2 dimensions: Serializer:SqlLayer.");

   [Fact] public async Task A_line_with_a_value_too_many_and_a_wildcard_fails_the_test_naming_the_line() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.ConfiguredByALineWithAValueTooManyAndAWildcard)))
        .Must().Be("Line 2 of the matrix configuration file TestUsingAValueTooManyAndAWildcard, \"Microsoft:*:Microsoft\", has 3 values, but the matrix has 2 dimensions: Serializer:SqlLayer.");

   [Fact] public async Task A_line_with_a_value_too_few_fails_the_test_naming_the_line() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.ConfiguredByALineWithAValueTooFew)))
        .Must().Be("Line 2 of the matrix configuration file TestUsingAValueTooFew, \"Microsoft\", has 1 value, but the matrix has 2 dimensions: Serializer:SqlLayer.");

   [Fact] public async Task A_name_that_is_not_a_value_of_its_dimension_fails_the_test_naming_the_dimensions_values() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.ConfiguredByALineWithANameThatIsNotAValueOfItsDimension)))
        .Must().Be("Line 2 of the matrix configuration file TestUsingAnInvalidDimensionValue, \"Microsoft:NotASqlLayer\", gives \"NotASqlLayer\" for the dimension SqlLayer, which has no value of that name. Its values are MsSql, Postgre, MySql.");

   [Fact] public async Task A_number_in_place_of_a_name_fails_the_test() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.ConfiguredByALineWithANumberForAValue)))
        .Must().Contain("gives \"7\" for the dimension SqlLayer, which has no value of that name");

   [Fact] public async Task A_comma_separated_list_of_names_in_place_of_a_name_fails_the_test() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.ConfiguredByALineWithACommaSeparatedListForAValue)))
        .Must().Contain("gives \"MsSql,Postgre\" for the dimension SqlLayer, which has no value of that name");

   [Fact] public async Task A_dimension_type_that_is_not_an_enum_fails_the_test() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.WithADimensionTypeThatIsNotAnEnum)))
        .Must().Be("Each dimension of a matrix must be an enum type, but String is not.");

   [Fact] public async Task A_matrix_with_no_dimensions_fails_the_test() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.WithNoDimensions)))
        .Must().Be("A matrix needs at least one dimension, but its matrix attribute declares no dimension enum types.");

   [Fact] public async Task Skipping_a_value_of_a_type_that_is_not_a_dimension_fails_the_test() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.SkippingAValueOfATypeThatIsNotADimension)))
        .Must().Be("MatrixTestsWithUnusableConfigurations.SkippingAValueOfATypeThatIsNotADimension skips Microsoft, a value of DIContainer, which is not one of the matrix's dimensions: Serializer:SqlLayer.");

   [Fact] public async Task Skipping_a_value_without_a_reason_fails_the_test() =>
      (await FailureMessageOf(nameof(MatrixTestsWithUnusableConfigurations.SkippingAValueWithoutAReason)))
        .Must().Be("Skipping Microsoft takes a reason, shown in the test runner, but its SkipAttribute gives none.");

   static async Task<string> FailureMessageOf(string matrixTestName)
   {
      var testCases = await MatrixDiscovery.DiscoverTestCases(typeof(MatrixTestsWithUnusableConfigurations), matrixTestName);

      testCases.Must().HaveCount(1);
      return testCases.Single().Must().BeExactType<ExecutionErrorTestCase>().Actual.ErrorMessage;
   }
}
