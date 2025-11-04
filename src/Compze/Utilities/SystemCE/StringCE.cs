using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.SystemCE;

///<summary>Contains extensions on <see cref="string"/></summary>
public static class StringCE
{
   internal const string Empty = "";

   ///<summary>returns true if me is null, empty or only whitespace</summary>
   [ContractAnnotation("null => true")]
   public static bool IsNullEmptyOrWhiteSpace(this string? @this) => string.IsNullOrWhiteSpace(@this);

   /// <summary>Delegates to <see cref="string.Join(string,string[])"/> </summary>
   public static string Join(this IEnumerable<string> @this, string separator)
   {
      Argument.NotNull(@this).NotNull(separator);

      return string.Join(separator, @this.ToArray());
   }

   public static string Join(this IEnumerable<string> @this) => string.Join("", @this.ToArray());

   public static string ReplaceOrdinal(this string @this, string oldValue, string newValue) => @this.Replace(oldValue, newValue, StringComparison.Ordinal);

   public static bool ContainsOrdinal(this string @this, string value) => @this.Contains(value, StringComparison.Ordinal);

   public static int GetHashcodeOrdinal(this string @this) => @this.GetHashCode(StringComparison.Ordinal);

   public static bool StartsWithOrdinal(this string @this, string ending) => @this.StartsWith(ending, StringComparison.Ordinal);

   public static bool EndsWithOrdinal(this string @this, string ending) => @this.EndsWith(ending, StringComparison.Ordinal);

   [StringFormatMethod(formatParameterName:"tessage")]
   public static string FormatInvariant(string message, params object[] arguments) =>
      string.Format(CultureInfo.InvariantCulture,  message, arguments);

   public static string RemoveLeadingLineBreak(this string @this)
   {
      while(@this.StartsWithOrdinal(Environment.NewLine))
      {
         @this = @this[Environment.NewLine.Length..];
      }

      return @this;
   }

   public static string Pluralize(this int count, string theString) => count == 1 ? theString : $"{theString}s";

   public static string Invariant(this FormattableString interpolatedString) => FormattableString.Invariant(interpolatedString);
}