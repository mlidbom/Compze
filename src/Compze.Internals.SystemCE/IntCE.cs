using System.Globalization;

namespace Compze.Internals.SystemCE;

public static class IntCE
{
   public static int ParseInvariant(string intAsString) => int.Parse(intAsString, CultureInfo.InvariantCulture);
   public static string ToStringInvariant(this int @this) => @this.ToString(CultureInfo.InvariantCulture);
   public static string ToStringInvariant(this int @this, string format) => @this.ToString(format, CultureInfo.InvariantCulture);
}