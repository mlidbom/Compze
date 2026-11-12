// ReSharper disable InconsistentNaming

namespace Compze.Internals.SystemCE.Tests;

/// <summary>
/// The line helpers split a string at every line ending, whichever the string has, and end the lines they return in<br/>
/// <see cref="Environment.NewLine"/>. A raw string literal holds the line endings of the source file it was compiled from,<br/>
/// so a package built on Linux and run on Windows hands them text whose lines end in LF.
/// </summary>
public class When_a_string_s_lines_end_in_both_LF_and_CRLF
{
   const string LinesEndingInBothLFAndCRLF = "first\nsecond\r\nthird";

   [XF] public void RemoveLinesWhere_removes_the_matching_line() =>
      LinesEndingInBothLFAndCRLF.RemoveLinesWhere(it => it == "second").Must().Be($"first{Environment.NewLine}third");

   [XF] public void IndentToDepth_indents_every_line() =>
      LinesEndingInBothLFAndCRLF.IndentToDepth("  ", 1).Must().Be($"  first{Environment.NewLine}  second{Environment.NewLine}  third");
}
