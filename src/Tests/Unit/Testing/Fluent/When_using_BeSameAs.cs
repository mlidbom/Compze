using System;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_BeSameAs : UniversalTestBase
{
   public class given_two_references_to_the_same_object : When_using_BeSameAs
   {
      readonly string _actual = "reference";
      readonly string _expected;

      public given_two_references_to_the_same_object() => _expected = _actual;

      [XF] public void BeSameAs_does_not_throw() => _actual.Must().BeSameAs(_expected);
   }

   public class given_two_different_objects_with_same_value : When_using_BeSameAs
   {
      readonly string _actual = new('a', 5);
      readonly string _expected = new('a', 5);

      public class BeSameAs_throws_AssertionFailedException : given_two_different_objects_with_same_value
      {
         [XF] public void when_objects_are_different_references()
            => Invoking(() => _actual.Must().BeSameAs(_expected)).Must().Throw<AssertionFailedException>();

         string ExceptionMessage() => Invoking(() => _actual.Must().BeSameAs(_expected))
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
                                            to be the same reference as the object "expected" returned by the expression:
                                            --------------------------------------------------
                                               _expected
                                            --------------------------------------------------
                                            but they are different objects (reference equality failed)
                                            --------------------------------------------------
                                            """);
      }
   }

   public class given_singleton_pattern : When_using_BeSameAs
   {
      class Singleton
      {
         static readonly Singleton _instance = new();
         Singleton() { }
         public static Singleton Instance => _instance;
      }

      [XF] public void BeSameAs_verifies_multiple_calls_return_same_instance()
      {
         var first = Singleton.Instance;
         var second = Singleton.Instance;
         
         first.Must().BeSameAs(second);
      }
   }

   public class given_string_interning : When_using_BeSameAs
   {
      [XF] public void BeSameAs_works_with_interned_strings()
      {
         var str1 = "literal";
         var str2 = "literal";
         
         str1.Must().BeSameAs(str2);
      }
   }
}
