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

   public static string RemoveLinesWhere(this string @this, Func<string, bool> predicate) => @this.Split(Environment.NewLine)
                                                                                                  .Where(it => !predicate(it))
                                                                                                  .JoinLines();

   public static string Pluralize(this int count, string theString) => count == 1 ? theString : $"{theString}s";
}
