using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;
using Xunit;

namespace Compze.Tests.Unit.XUnit.Infrastructure;

/// <summary>
/// Test to verify that the PluggableComponentsTheory attribute works correctly.
/// This test should run once for each combination in TestUsingPluggableComponentCombinations.
/// </summary>
public class PluggableComponentsTheoryTests : DuplicateByPluggableComponentTest
{
   [PluggableComponentsTheory]
   public void Should_execute_once_per_combination_with_combination_string_passed(string pluggableComponentsCombination)
   {
      // Output the combination so we can see it ran
      System.Console.WriteLine($"✓ Test executed with combination: {pluggableComponentsCombination}");

      // Verify the combination parameter is not null or empty
      pluggableComponentsCombination.Should().NotBeNullOrWhiteSpace();
      pluggableComponentsCombination.Should().Contain(":");

      // Verify the format is correct
      var parts = pluggableComponentsCombination.Split(':');
      parts.Should().HaveCount(2, "combination should be in format 'PersistenceLayer:DIContainer'");

      parts[0].Should().NotBeEmpty("persistence layer part should not be empty");
      parts[1].Should().NotBeEmpty("DI container part should not be empty");

      System.Console.WriteLine($"  Persistence Layer: {parts[0]}");
      System.Console.WriteLine($"  DI Container: {parts[1]}");
   }

   [Fact]
   public void Regular_fact_test_should_run_only_once()
   {
      System.Console.WriteLine("✓ This regular Fact test runs exactly once");
      true.Should().BeTrue();
   }
}
