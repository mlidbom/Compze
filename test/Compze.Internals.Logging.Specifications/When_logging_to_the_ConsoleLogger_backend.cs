// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

// Tests redirect Console.Out (global state) so they must run sequentially.
// Keeping them in a single flat class — xUnit runs tests within a class sequentially.
public class When_logging_to_the_ConsoleLogger_backend
{
   static string Capture(Action<ILogger> log)
   {
      var capturedConsoleOutput = new StringWriter();
      var originalConsoleOut = Console.Out;
      Console.SetOut(capturedConsoleOutput);
      try
      {
         log(ConsoleLogger.Create(typeof(When_logging_to_the_ConsoleLogger_backend)));
         return capturedConsoleOutput.ToString();
      }
      finally
      {
         Console.SetOut(originalConsoleOut);
      }
   }

   [XF] public void a_template_with_a_single_hole_substitutes_the_holes_value_into_the_rendered_text()
      => Capture(log => { var userId = 42; log.Info($"user {userId} signed in"); })
        .Must().Contain("user 42 signed in");

   [XF] public void a_format_specifier_controls_how_the_value_is_rendered()
      => Capture(log => { var ratio = 0.456; log.Info($"ratio={ratio:F2}"); })
        .Must().Contain("ratio=0.46");

   [XF] public void a_positive_alignment_right_pads_the_value_to_the_specified_width()
      => Capture(log => { var count = 3; log.Info($">{count,5}<"); })
        .Must().Contain(">    3<");

   [XF] public void a_negative_alignment_left_pads_the_value_to_the_specified_width()
      => Capture(log => { var count = 3; log.Info($">{count,-5}<"); })
        .Must().Contain(">3    <");

   [XF] public void alignment_and_format_specifier_combine_correctly()
      => Capture(log => { var ratio = 0.4567; log.Info($">{ratio,8:F2}<"); })
        .Must().Contain(">    0.46<");

   [XF] public void braces_in_a_plain_string_message_appear_unchanged_in_the_output()
      => Capture(log => log.Info("literal {not a hole}"))
        .Must().Contain("literal {not a hole}");

   [XF] public void a_template_with_no_holes_renders_as_the_literal_text()
      => Capture(log => log.Info($"just a literal"))
        .Must().Contain("just a literal");

   [XF] public void null_values_render_as_the_literal_text_open_paren_null_close_paren()
      => Capture(log => { string? value = null; log.Info($"got {value}"); })
        .Must().Contain("got (null)");
}
