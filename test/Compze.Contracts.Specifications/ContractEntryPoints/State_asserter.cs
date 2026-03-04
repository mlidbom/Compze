using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.ContractEntryPoints;

public class State_asserter
{
   public class on_Assert_failure : State_asserter
   {
      [XF] public void throws_StateAssertionFailedException() =>
         Invoking(() => Contract.State.Assert(false)).Must().Throw<StateAssertionFailedException>();
   }

   public class on_NotNull_with_null_value : State_asserter
   {
      static readonly object? NullValue = null;

      [XF] public void throws_StateAssertionFailedException() =>
         Invoking(() => Contract.State.NotNull(NullValue)).Must().Throw<StateAssertionFailedException>();
   }
}
