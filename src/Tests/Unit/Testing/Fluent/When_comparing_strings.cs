using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_comparing_strings : UniversalTestBase
{
   public class given_two_multiline_strings : When_comparing_strings
   {
      readonly string _actual = """
                                First line
                                Actual Second line
                                Third line
                                """;

      public class that_differ : given_two_multiline_strings
      {
         readonly string _expected = """
                                     First line
                                     Expected Second line
                                     Third line
                                     """;

         public class must_be_throws_AssertionFailedException : that_differ
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

   public class given_two_single_line_strings : When_comparing_strings
   {
      readonly string _actual = """First Actual-Second Third""";

      public class that_differ : given_two_single_line_strings
      {
         readonly string _expected = """First Expected-Second Third""";

         public class must_be_throws_AssertionFailedException : that_differ
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
