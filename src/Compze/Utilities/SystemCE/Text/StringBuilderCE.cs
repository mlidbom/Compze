using System.Globalization;
using System.Text;

namespace Compze.Utilities.SystemCE.Text;

static class StringBuilderCE
{
   public static void AppendInvariant(this StringBuilder @this,
                                      ref StringBuilder.AppendInterpolatedStringHandler handler) =>
      @this.Append(CultureInfo.InvariantCulture, ref handler);
}
