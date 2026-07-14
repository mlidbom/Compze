

// ReSharper disable InconsistentNaming

using Compze.Must.Assertions;

namespace Compze.Must.Specifications;

public class When_calling_Invoking_Must_Throw : UniversalTestBase
{
   public class given_an_action_that_throws_the_expected_exception : When_calling_Invoking_Must_Throw
   {
      readonly InvalidOperationException _actual = new("test message");

      [XF] public void and_allows_asserting_on_the_exception()
         => Invoking(() => throw _actual)
           .Must().Throw<InvalidOperationException>()
           .Which.Must().Be(_actual);
   }

   public class given_an_action_that_throws_a_different_exception : When_calling_Invoking_Must_Throw
   {
      public class Throw_throws : given_an_action_that_throws_a_different_exception
      {
         [XF] public void when_wrong_exception_type_is_thrown()
            => Invoking(() => Invoking(() => throw new InvalidOperationException("wrong"))
                             .Must().Throw<ArgumentException>())
              .Must().Throw<AssertionFailedException>();

         public class and_the_exception_message : Throw_throws
         {
            static string ExceptionMessage() => Invoking(() => Invoking(() => throw new InvalidOperationException("wrong"))
                                                              .Must().Throw<ArgumentException>())
                                               .Must().Throw<AssertionFailedException>()
                                               .Which.Message;

            [XF] public void is_the_full_formatted_message()
               => ExceptionMessage().Must().Be("""

                                               --------------------------------------------------
                                               Failing assertion:
                                               --------------------------------------------------
                                               Invoking(() => throw new InvalidOperationException("wrong")).Must().Throw<ArgumentException>()
                                               --------------------------------------------------
                                               Expected a System.ArgumentException 
                                               but got a System.InvalidOperationException
                                               --------------------------------------------------
                                               """);
         }
      }
   }

   public class given_an_action_that_does_not_throw : When_calling_Invoking_Must_Throw
   {
      public class Throw_throws : given_an_action_that_does_not_throw
      {
         [XF] public void when_no_exception_is_thrown()
            => Invoking(() => Invoking(() =>
                              { /* do nothing */
                              })
                             .Must().Throw<InvalidOperationException>())
              .Must().Throw<AssertionFailedException>();

         public class and_the_exception_message : Throw_throws
         {
            static string ExceptionMessage() => Invoking(() => Invoking(() =>
                                                               { /* do nothing */
                                                               })
                                                              .Must()
                                                              .Throw<InvalidOperationException>())
                                               .Must()
                                               .Throw<AssertionFailedException>()
                                               .Which
                                               .Message;

            [XF] public void is_the_full_formatted_message()
               => ExceptionMessage().Must().Be("""

                                               --------------------------------------------------
                                               Failing assertion:
                                               --------------------------------------------------
                                               Invoking(() =>
                                               { /* do nothing */
                                               }).Must().Throw<InvalidOperationException>()
                                               --------------------------------------------------
                                               Expected a System.InvalidOperationException, but no exception was thrown
                                               --------------------------------------------------
                                               """);
         }
      }
   }

   public class given_an_action_that_throws_a_derived_exception : When_calling_Invoking_Must_Throw
   {
      public class Throw_catches_the_derived_exception : given_an_action_that_throws_a_derived_exception
      {
         // ReSharper disable once NotResolvedInText
         readonly ArgumentNullException _actual = new("argument");

         [XF] public void when_expecting_base_exception_type()
            => Invoking(() => throw _actual)
              .Must().Throw<ArgumentException>()
              .Which.Must().Be(_actual);
      }
   }
}
