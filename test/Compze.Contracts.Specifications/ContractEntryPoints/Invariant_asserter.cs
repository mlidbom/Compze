using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.ContractEntryPoints;

public class Invariant_asserter
{
   public class on_Assert_failure : Invariant_asserter
   {
      [XF] public void throws_InvariantAssertionFailedException() =>
         Invoking(() => Contract.Invariant.Assert(false)).Must().Throw<InvariantAssertionFailedException>();
   }

   public class on_NotNull_with_null_value : Invariant_asserter
   {
      static readonly object? NullValue = null;

      [XF] public void throws_InvariantAssertionFailedException() =>
         Invoking(() => Contract.Invariant.NotNull(NullValue)).Must().Throw<InvariantAssertionFailedException>();
   }
}
