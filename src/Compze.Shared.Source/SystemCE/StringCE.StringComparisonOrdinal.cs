using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Compze.Utilities.SystemCE;

///<summary>Contains extensions on <see cref="string"/></summary>
internal static partial class StringCE
{
   public static string ReplaceCE(this string @this, string oldValue, string newValue) => @this.Replace(oldValue, newValue, StringComparison.Ordinal);

   public static bool ContainsCE(this string @this, string value) => @this.Contains(value, StringComparison.Ordinal);

   public static int GetHashcodeCE(this string @this) => @this.GetHashCode(StringComparison.Ordinal);

   public static bool StartsWithCE(this string @this, string ending) => @this.StartsWith(ending, StringComparison.Ordinal);

   public static bool EndsWithCE(this string @this, string ending) => @this.EndsWith(ending, StringComparison.Ordinal);

   [StringFormatMethod(formatParameterName:"tessage")]
   public static string FormatInvariant(string message, params object[] arguments) =>
      string.Format(CultureInfo.InvariantCulture,  message, arguments);

   public static string Invariant(this FormattableString interpolatedString) => FormattableString.Invariant(interpolatedString);
}