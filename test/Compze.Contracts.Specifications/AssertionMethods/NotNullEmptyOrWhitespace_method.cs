using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class NotNullEmptyOrWhitespace_method : AssertionMethodsTest
{
   static readonly string? NullString = null;

   public class called_with_null_string : NotNullEmptyOrWhitespace_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace(NullString)).Must().Throw<AssertionTestException>();
   }

   public class called_with_empty_string : NotNullEmptyOrWhitespace_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace("")).Must().Throw<AssertionTestException>();
   }

   public class called_with_whitespace_only_string : NotNullEmptyOrWhitespace_method
   {
      [XF] public void throws_for_spaces() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace(" ")).Must().Throw<AssertionTestException>();

      [XF] public void throws_for_tabs() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace("\t")).Must().Throw<AssertionTestException>();

      [XF] public void throws_for_newline() =>
         Invoking(() => Asserter.NotNullEmptyOrWhitespace(Environment.NewLine)).Must().Throw<AssertionTestException>();
   }

   public class called_with_non_whitespace_string : NotNullEmptyOrWhitespace_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.NotNullEmptyOrWhitespace("hello").Must().Be(Asserter);
   }
}
