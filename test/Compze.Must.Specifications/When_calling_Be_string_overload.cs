

// ReSharper disable InconsistentNaming

#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Must.Specifications;

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
                                            Failing assertion:
                                            --------------------------------------------------
                                            _actual.Must().Be(_expected)
                                            --------------------------------------------------
                                            Diff:
                                            --------------------------------------------------
                                            --- expected
                                            +++ actual
                                            @@ -1,3 +1,3 @@
                                             First line
                                            -Expected Second line
                                            +Actual Second line
                                             Third line
                                            
                                            --------------------------------------------------
                                            _actual was a string with the value:
                                            --------------------------------------------------
                                            First line
                                            Actual Second line
                                            Third line
                                            --------------------------------------------------
                                            _expected was a string with the value:
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
                                            Failing assertion:
                                            --------------------------------------------------
                                            _actual.Must().Be(_expected)
                                            --------------------------------------------------
                                            Diff:
                                            --------------------------------------------------
                                            First [-Expected-Second] Third
                                            First [+Actual-Second] Third
                                            --------------------------------------------------
                                            _actual was a string with the value:
                                            --------------------------------------------------
                                            First Actual-Second Third
                                            --------------------------------------------------
                                            _expected was a string with the value:
                                            --------------------------------------------------
                                            First Expected-Second Third
                                            --------------------------------------------------
                                            """);
         }
      }
   }
}
