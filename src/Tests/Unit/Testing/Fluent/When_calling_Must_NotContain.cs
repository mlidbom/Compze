using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___String = Compze.Utilities.Testing.Fluent.Must___String;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

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
