namespace Compze.Internals.SystemCE;

///<summary>Contains extensions on <see cref="string"/></summary>
public static class StringIndenter
{
   /// <summary>
   /// Each line of the string indented by <paramref name="depth"/> times <paramref name="indent"/>, its lines ending in <see cref="Environment.NewLine"/>.<br/>
   /// A line ends at any line ending, whichever the string has, as in <see cref="StringCE.RemoveLinesWhere"/>.
   /// </summary>
   public static string IndentToDepth(this string it, string indent, int depth) => it.ReplaceLineEndings().Split(Environment.NewLine).Select(line => Enumerable.Repeat(indent, depth).Join(string.Empty) + line).Join(Environment.NewLine);
   static string IndentSpaces(this string it, int count = 3) => it.IndentToDepth(" ", count);
   public static string Indent(this string it) => it.IndentSpaces();
   public static string JoinLines(this IEnumerable<string> it) => it.Join(Environment.NewLine);
}
