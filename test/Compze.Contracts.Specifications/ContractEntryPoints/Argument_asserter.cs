using System;
using Compze.Contracts.Exceptions;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.ContractEntryPoints;

public class Argument_asserter
{
   public class on_Assert_failure : Argument_asserter
   {
      [XF] public void throws_ArgumentAssertionFailedException() =>
         Invoking(() => Contract.Argument.Assert(false)).Must().Throw<ArgumentAssertionFailedException>();
   }

   public class on_NotNull_with_null_value : Argument_asserter
   {
      static readonly object? NullValue = null;

      [XF] public void throws_ArgumentNullException() =>
         Invoking(() => Contract.Argument.NotNull(NullValue)).Must().Throw<ArgumentNullException>();

      [XF] public void the_ParamName_is_the_argument_expression() =>
         Invoking(() => Contract.Argument.NotNull(NullValue))
            .Must().Throw<ArgumentNullException>()
            .Which.ParamName!.Must().Be(nameof(NullValue));
   }

   public class on_NotDefault_failure : Argument_asserter
   {
      [XF] public void throws_ArgumentAssertionFailedException() =>
         Invoking(() => Contract.Argument.NotDefault(default(Guid))).Must().Throw<ArgumentAssertionFailedException>();
   }

   public class assertion_methods_can_be_chained_across_asserter_types : Argument_asserter
   {
      [XF] public void Argument_chains_to_State_chains_to_Invariant() =>
         Contract.Argument.Assert(true).State.Assert(true).Invariant.Assert(true);
   }
}
