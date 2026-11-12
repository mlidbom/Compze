using JetBrains.Annotations;

namespace Compze.Internals.SystemCE;

///<summary>Contains extensions on <see cref="string"/></summary>
public static partial class StringCE
{
   public static string Join(this IEnumerable<string> @this) => string.Join("", @this.ToArray());

   ///<summary>returns true if me is null, empty or only whitespace</summary>
   [ContractAnnotation("null => true")]
   public static bool IsNullEmptyOrWhiteSpace(this string? @this) => string.IsNullOrWhiteSpace(@this);

   /// <summary>Delegates to <see cref="string.Join(string,string[])"/> </summary>
   public static string Join(this IEnumerable<string> @this, string separator) => string.Join(separator, @this);

   /// <summary>
   /// The string without the lines that <paramref name="predicate"/> matches, its lines ending in <see cref="Environment.NewLine"/>.<br/>
   /// A line ends at any line ending, whichever the string has: text from a raw string literal holds the line endings of the<br/>
   /// source file it was compiled from, which needn't be the line endings of the operating system it runs on.
   /// </summary>
   public static string RemoveLinesWhere(this string @this, Func<string, bool> predicate) => @this.ReplaceLineEndings()
                                                                                                  .Split(Environment.NewLine)
                                                                                                  .Where(it => !predicate(it))
                                                                                                  .JoinLines();

   public static string Pluralize(this int count, string theString) => count == 1 ? theString : $"{theString}s";
}
