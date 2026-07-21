namespace Compze.xUnitMatrix._private;

///<summary>
/// Thrown when <see cref="MatrixCombination.Current"/> is accessed while no matrix test is running on the current async context.
/// <see cref="MatrixCombination.Current"/> is only available within the test class constructor or test method of a test driven by
/// a <see cref="MatrixTheoryAttribute"/> subclass; reaching it from anywhere else — or from a different async context than the
/// one the test runs on — is a usage error, not a recoverable condition.
///</summary>
class NoCurrentMatrixCombinationException() : Exception(
   $"There is no current {nameof(MatrixCombination)} on this async context. {nameof(MatrixCombination)}.{nameof(MatrixCombination.Current)} is only available "
 + $"while a matrix test is executing: within the test class constructor or the test method of a test marked with a {nameof(MatrixTheoryAttribute)} subclass.");
