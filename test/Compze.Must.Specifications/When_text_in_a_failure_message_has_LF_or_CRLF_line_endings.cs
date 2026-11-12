// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

/// <summary>
/// Every line of a failure message ends in <see cref="Environment.NewLine"/>, whatever line endings the text it was made of had,<br/>
/// and text with line endings of either kind is shown as lines. Must's own text comes from raw string literals, which hold the<br/>
/// line endings of the source files Must was compiled from, and the released package is compiled on Linux: run on Windows, its<br/>
/// lines ended in LF among lines that ended in CRLF, and the placeholder for a line to leave out, which only a line ending in<br/>
/// <see cref="Environment.NewLine"/> was recognized as, showed.
/// </summary>
public class When_text_in_a_failure_message_has_LF_or_CRLF_line_endings : UniversalTestBase
{
   const string LinesEndingInBothLFAndCRLF = "first\nsecond\r\nthird";
   static readonly string TheSameLinesEndingInNewLine = $"first{Environment.NewLine}second{Environment.NewLine}third";

   [XF] public void the_lines_of_a_custom_assertion_s_failure_message_end_in_NewLine() =>
      Invoking(() => 1.Must().RunAssertion(_ => false, failureMessage: _ => LinesEndingInBothLFAndCRLF))
        .Must().Throw<AssertionFailedException>()
        .Which.Message.Must().Contain(TheSameLinesEndingInNewLine);

   [XF] public void the_lines_of_a_custom_assertion_s_whole_message_end_in_NewLine() =>
      Invoking(() => 1.Must().RunAssertion(_ => false, messageOverride: _ => LinesEndingInBothLFAndCRLF))
        .Must().Throw<AssertionFailedException>()
        .Which.Message.Must().Be($"{Environment.NewLine}{TheSameLinesEndingInNewLine}");

   [XF] public void the_lines_of_the_subject_s_expression_lose_the_indentation_they_share() =>
      Invoking(() => 1.Must("first\n      second\r\n      third").Be(2))
        .Must().Throw<AssertionFailedException>()
        .Which.Message.Must().Contain($"{TheSameLinesEndingInNewLine}.Must().Be(2)");

   [XF] public void two_strings_whose_lines_end_in_LF_that_differ_show_a_diff_of_their_lines() =>
      Invoking(() => "first\nactual second\nthird".Must().Be("first\nexpected second\nthird"))
        .Must().Throw<AssertionFailedException>()
        .Which.Message.Must().Contain("""
                                      @@ -1,3 +1,3 @@
                                       first
                                      -expected second
                                      +actual second
                                       third
                                      """);
}
