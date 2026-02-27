using System;

namespace Compze.Contracts;

public static class Contract
{
   ///<summary>Assert conditions about current state of "this". Failures throw <see cref="InvalidOperationException"/>.</summary>
   public static ContractAsserter State { get; } = new("State",
                                                        message => new InvalidOperationException(message),
                                                        message => new InvalidOperationException(message));

   ///<summary>Assert something that must always be true for "this". Failures throw <see cref="InvariantViolatedException"/></summary>
   public static ContractAsserter Invariant { get; } = new("Invariant",
                                                           message => new InvariantViolatedException(message),
                                                           message => new InvariantViolatedException(message));

   ///<summary>Assert conditions on arguments to the current method. Failures throw <see cref="ArgumentException"/></summary>
   public static ContractAsserter Argument { get; } = new("Argument",
                                                          message => new ArgumentException(message),
                                                          message => new ArgumentNullException(message));
}
