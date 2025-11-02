using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must_Be___Null___strings = Compze.Utilities.Testing.Fluent.Must_Be___Null___strings;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_NotBeNullOrEmpty : UniversalTestBase
{
   public class with_non_empty_string : When_calling_Must_NotBeNullOrEmpty
   {
      [XF] public void it_does_not_throw() => Must_Be___Null___strings.NotBeNullOrEmpty(__Must.Must("text"));
   }

   public class with_null : When_calling_Must_NotBeNullOrEmpty
   {
      readonly string? _value = null;
      [XF] public void it_throws() => Invoking(() => Must_Be___Null___strings.NotBeNullOrEmpty(__Must.Must(_value)))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_empty_string : When_calling_Must_NotBeNullOrEmpty
   {
      [XF] public void it_throws() => Invoking(() => Must_Be___Null___strings.NotBeNullOrEmpty(__Must.Must(string.Empty)))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
