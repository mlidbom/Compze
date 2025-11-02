using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must_Be_string = Compze.Utilities.Testing.Fluent.Must_Be_string;

// ReSharper disable InconsistentNaming

#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Be_string_overload : UniversalTestBase
{
   public class with_two_multiline_strings : When_calling_Be_string_overload
   {
      readonly string _actual = """
                                First line
                                Actual Second line
                                Third line
                                """;

      public class that_differ : with_two_multiline_strings
      {
         readonly string _expected = """
                                     First line
                                     Expected Second line
                                     Third line
                                     """;

         public class it_throws_AssertionFailedException : that_differ
         {
            string ExceptionMessage() => Invoking(() => _actual.Must().Be(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

            [XF] public void and_the_full_message_is() =>
               ExceptionMessage().Must().Be("""

                                            --------------------------------------------------
                                            the expression: 
                                            --------------------------------------------------
                                               _actual
                                            --------------------------------------------------
                                            did not result in the expected string, producing the diff
                                            --------------------------------------------------
                                            --- expected
                                            +++ actual
                                            @@ -1,3 +1,3 @@
                                             First line
                                            -Expected Second line
                                            +Actual Second line
                                             Third line

                                            --------------------------------------------------
                                            Actual was:
                                            --------------------------------------------------
                                            First line
                                            Actual Second line
                                            Third line
                                            --------------------------------------------------
                                            Expected was:
                                            --------------------------------------------------
                                            First line
                                            Expected Second line
                                            Third line
                                            --------------------------------------------------
                                            """);
         }
      }
   }

   public class with_two_single_line_strings : When_calling_Be_string_overload
   {
      readonly string _actual = """First Actual-Second Third""";

      public class that_differ : with_two_single_line_strings
      {
         readonly string _expected = """First Expected-Second Third""";

         public class it_throws_AssertionFailedException : that_differ
         {
            string ExceptionMessage() => Invoking(() => _actual.Must().Be(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

            [XF] public void and_the_full_message_is() =>
               ExceptionMessage().Must().Be("""
                                            
                                            --------------------------------------------------
                                            the expression: 
                                            --------------------------------------------------
                                               _actual
                                            --------------------------------------------------
                                            did not result in the expected string, producing the diff
                                            --------------------------------------------------
                                            First [-Expected-Second] Third
                                            First [+Actual-Second] Third
                                            --------------------------------------------------
                                            Actual was:
                                            --------------------------------------------------
                                            First Actual-Second Third
                                            --------------------------------------------------
                                            Expected was:
                                            --------------------------------------------------
                                            First Expected-Second Third
                                            --------------------------------------------------
                                            """);
         }
      }
   }
}
