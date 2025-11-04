using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Must;

public class When_calling_Must_NotContain : UniversalTestBase
{
   public class with_a_string_that_does_not_contain_the_substring : When_calling_Must_NotContain
   {
      [XF] public void it_does_not_throw() => "hello world".Must().NotContain("xyz");
   }

   public class with_a_string_that_contains_the_substring : When_calling_Must_NotContain
   {
      [XF] public void it_throws() => Invoking(() => "hello world".Must().NotContain("world"))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_empty_string : When_calling_Must_NotContain
   {
      [XF] public void it_does_not_throw_when_looking_for_non_empty() => "".Must().NotContain("xyz");
   }
}
