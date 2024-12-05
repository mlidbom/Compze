#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace Compze.Contracts;

static class Assert
{
   ///<summary>Assert conditions about current state of "this". Failures would mean that someone made a call that is illegal given state of "this".</summary>
   public static ContractAssertion State { get; } = new StateAssertion();

   ///<summary>Assert something that must always be true for "this".</summary>
   public static ContractAssertion Invariant { get; } = new InvariantAssertion();

   ///<summary>Assert conditions on arguments to current method.</summary>
   public static ContractAssertion Argument { get; } = new ArgumentAssertion();

   ///<summary>Assert conditions on the result of making a method call.</summary>
   public static ContractAssertion Result { get; } = new ResultAssertion();
}