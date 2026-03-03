using System;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.SystemCE;

///<summary>Contains extensions on <see cref="string"/></summary>
public static class StringIndenter
{
   public static string IndentToDepth(this string it, string indent, int depth) => it.Split(Environment.NewLine).Select(line => Enumerable.Repeat(indent, depth).Join(string.Empty) + line).Join(Environment.NewLine);
   static string IndentSpaces(this string it, int count = 3) => it.IndentToDepth(" ", count);
   public static string Indent(this string it) => it.IndentSpaces();
   public static string JoinLines(this IEnumerable<string> it) => it.Join(Environment.NewLine);
}
