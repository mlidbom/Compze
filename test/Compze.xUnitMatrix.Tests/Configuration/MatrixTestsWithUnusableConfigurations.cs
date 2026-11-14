using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests.Configuration;

///<summary>
/// Matrix tests whose configurations cannot be used, for <see cref="WhenTheConfigurationCannotBeUsed"/> to discover.<br/>
/// Abstract, so that xUnit does not discover and run them itself: they would fail, which is what the specification checks.
///</summary>
public abstract class MatrixTestsWithUnusableConfigurations
{
   [MissingConfigurationFileMatrix] public void ConfiguredByAFileThatDoesNotExist() {}

   [EmptyConfigurationFileMatrix] public void ConfiguredByAFileWithNoCombinations() {}

   [AValueTooManyMatrix] public void ConfiguredByALineWithAValueTooMany() {}

   [AValueTooManyAndAWildcardMatrix] public void ConfiguredByALineWithAValueTooManyAndAWildcard() {}

   [AValueTooFewMatrix] public void ConfiguredByALineWithAValueTooFew() {}

   [InvalidDimensionValueMatrix] public void ConfiguredByALineWithANameThatIsNotAValueOfItsDimension() {}

   [ANumberForAValueMatrix] public void ConfiguredByALineWithANumberForAValue() {}

   [ACommaSeparatedListForAValueMatrix] public void ConfiguredByALineWithACommaSeparatedListForAValue() {}

   [ADimensionTypeThatIsNotAnEnumMatrix] public void WithADimensionTypeThatIsNotAnEnum() {}

   [NoDimensionsMatrix] public void WithNoDimensions() {}

   [TwoComponentMatrix]
   [Skip<DIContainer>(DIContainer.Microsoft, "DIContainer is not one of the matrix's dimensions")]
   public void SkippingAValueOfATypeThatIsNotADimension() {}

   [TwoComponentMatrix]
   [Skip<Serializer>(Serializer.Microsoft, "")]
   public void SkippingAValueWithoutAReason() {}
}
