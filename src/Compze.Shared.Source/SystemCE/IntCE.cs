using System.Globalization;

namespace Compze.Utilities.SystemCE;

internal static class IntCE
{
   public static int ParseInvariant(string intAsString) => int.Parse(intAsString, CultureInfo.InvariantCulture);
   public static string ToStringInvariant(this int @this) => @this.ToString(CultureInfo.InvariantCulture);
}