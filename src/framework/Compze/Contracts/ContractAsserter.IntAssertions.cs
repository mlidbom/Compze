using System.Runtime.CompilerServices;

namespace Compze.Contracts;

partial class ContractAsserter
{
   public ContractAsserter IsGreaterThan(int value, int lowerBound, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value > lowerBound ? this : throw _createException($"{valueString} was {value} which is not greater than {lowerBound}");
}
