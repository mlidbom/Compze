using System;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_NotBeSameAs : UniversalTestBase
{
   public class given_two_different_objects : When_using_NotBeSameAs
   {
      readonly string _actual = new('a', 5);
      readonly string _unexpected = new('a', 5);

      [XF] public void NotBeSameAs_does_not_throw() => _actual.Must().NotBeSameAs(_unexpected);
   }

   public class given_two_references_to_the_same_object : When_using_NotBeSameAs
   {
      readonly string _actual = "reference";
      readonly string _unexpected;

      public given_two_references_to_the_same_object() => _unexpected = _actual;

      public class NotBeSameAs_throws_AssertionFailedException : given_two_references_to_the_same_object
      {
         [XF] public void when_objects_are_same_reference()
            => Invoking(() => _actual.Must().NotBeSameAs(_unexpected)).Must().Throw<AssertionFailedException>();

         string ExceptionMessage() => Invoking(() => _actual.Must().NotBeSameAs(_unexpected))
                                     .Must()
                                     .Throw<AssertionFailedException>()
                                     .Which
                                     .Message;

         [XF] public void is_the_full_formatted_message()
            => ExceptionMessage().Must().Be("""
                                            --------------------------------------------------
                                            expected the object "it" returned by the expression: 
                                            --------------------------------------------------
                                               _actual
                                            --------------------------------------------------
                                            to not be the same reference as the object "unexpected" returned by the expression:
                                            --------------------------------------------------
                                               _unexpected
                                            --------------------------------------------------
                                            but they reference the same object (reference equality succeeded when it shouldn't)
                                            --------------------------------------------------
                                            """);
      }
   }

   public class given_different_instances_with_equal_values : When_using_NotBeSameAs
   {
      class ValueObject
      {
         public ValueObject(int value) => Value = value;
         public int Value { get; }
         public override bool Equals(object? obj) => obj is ValueObject other && Value == other.Value;
         public override int GetHashCode() => Value.GetHashCode();
      }

      [XF] public void NotBeSameAs_succeeds_even_when_values_are_equal()
      {
         var obj1 = new ValueObject(42);
         var obj2 = new ValueObject(42);
         
         obj1.Must().NotBeSameAs(obj2);
      }
   }
}
