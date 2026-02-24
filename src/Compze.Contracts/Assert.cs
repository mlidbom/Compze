using System;

namespace Compze.Contracts;

public static class Assert
{
   ///<summary>Assert conditions about current state of "this". Failures throw <see cref="InvalidOperationException"/>.</summary>
   public static ContractAsserter State { get; } = new(tessage => new InvalidOperationException(tessage));

   ///<summary>Assert something that must always be true for "this". Failures throw <see cref="InvariantViolatedException"/></summary>
   public static ContractAsserter Invariant { get; } = new(tessage => new InvariantViolatedException(tessage));

   ///<summary>Assert conditions on arguments to the current method. Failures throw <see cref="ArgumentException"/></summary>
   public static ContractAsserter Argument { get; } = new(tessage => new ArgumentException(tessage));

   ///<summary>Assert conditions on the results of a method before returning them. Failures throw <see cref="InvalidResultException"/> </summary>
   public static ContractAsserter ReturnValue { get; } = new(tessage => new InvalidResultException(tessage));
}
