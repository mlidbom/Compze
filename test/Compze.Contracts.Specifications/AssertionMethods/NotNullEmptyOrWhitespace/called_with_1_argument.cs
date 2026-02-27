using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullEmptyOrWhitespace;

public class called_with_1_argument : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_it_is_non_whitespace() =>
      Asserter.NotNullEmptyOrWhitespace("hello").Must().Be(Asserter);

   [XF] public void throws_with_message_containing_the_argument_expression()
   {
      string? nullArg = null;
      MustThrowContaining(() => Asserter.NotNullEmptyOrWhitespace(nullArg), nameof(nullArg));
   }

   public class throws_if : called_with_1_argument
   {
      static readonly string? NullArg = null;

      [XF] public void it_is_null() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace(NullArg)).Must().Throw<AssertionTestException>();

      [XF] public void it_is_empty() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace("")).Must().Throw<AssertionTestException>();

      [XF] public void it_is_spaces() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace(" ")).Must().Throw<AssertionTestException>();

      [XF] public void it_is_tabs() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace("\t")).Must().Throw<AssertionTestException>();

      [XF] public void it_is_newline() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace(Environment.NewLine)).Must().Throw<AssertionTestException>();
   }
}
