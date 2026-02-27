using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Compze.Utilities.SystemCE;

///<summary>Contains extensions on <see cref="string"/></summary>
public static partial class StringCE
{
   extension(string @this)
   {
      public string ReplaceCE(string oldValue, string newValue) => @this.Replace(oldValue, newValue, StringComparison.Ordinal);
      public bool ContainsCE(string value) => @this.Contains(value, StringComparison.Ordinal);
      public int GetHashcodeCE() => @this.GetHashCode(StringComparison.Ordinal);
      public bool StartsWithCE(string ending) => @this.StartsWith(ending, StringComparison.Ordinal);
      public bool EndsWithCE(string ending) => @this.EndsWith(ending, StringComparison.Ordinal);
      public int IndexOfOrdinal(char character) => @this.IndexOf(character, StringComparison.Ordinal);
   }

   [StringFormatMethod(formatParameterName: "tessage")]
   public static string FormatInvariant(string message, params object[] arguments) =>
      string.Format(CultureInfo.InvariantCulture, message, arguments);

   public static string Invariant(this FormattableString interpolatedString) => FormattableString.Invariant(interpolatedString);
}
