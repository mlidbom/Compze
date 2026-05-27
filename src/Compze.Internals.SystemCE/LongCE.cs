using System.Globalization;

namespace Compze.Internals.SystemCE;

public static class LongCE
{
   public static long ParseInvariant(string longAsString) => long.Parse(longAsString, CultureInfo.InvariantCulture);
   public static string ToStringInvariant(this long @this) => @this.ToString(CultureInfo.InvariantCulture);
   public static string ToStringInvariant(this long @this, string format) => @this.ToString(format, CultureInfo.InvariantCulture);
}
