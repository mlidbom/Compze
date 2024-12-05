using System;

namespace Compze.Contracts;

static class Assert
{
   ///<summary>Assert conditions about current state of "this". Failures throw <see cref="InvalidOperationException"/>.</summary>
   public static ContractAsserter State { get; } = new(message => new InvalidOperationException(message));

   ///<summary>Assert something that must always be true for "this". Failures throw <see cref="InvariantViolatedException"/></summary>
   public static ContractAsserter Invariant { get; } = new(message => new InvariantViolatedException(message));

   ///<summary>Assert conditions on arguments to the current method. Failures throw <see cref="ArgumentException"/></summary>
   public static ContractAsserter Argument { get; } = new(message => new ArgumentException(message));

   ///<summary>Assert conditions on the results of a method before returning them. Failures throw <see cref="InvalidResultException"/> </summary>
   public static ContractAsserter Result { get; } = new(message => new InvalidResultException(message));
}
