using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
// ReSharper disable MethodOverloadWithOptionalParameter
// ReSharper disable UnusedMember.Global

namespace Compze.Internals.SystemCE.TextCE;

public static class StringBuilderCE
{
   public static StringBuilder AppendInvariant(this StringBuilder @this,
                                               [InterpolatedStringHandlerArgument(nameof(@this))] ref AppendInvariantHandler _) => @this;

   [InterpolatedStringHandler]
   public ref struct AppendInvariantHandler(int literalLength, int formattedCount, StringBuilder stringBuilder)
   {
      StringBuilder.AppendInterpolatedStringHandler _handler = new(literalLength, formattedCount, stringBuilder, CultureInfo.InvariantCulture);

      public void AppendLiteral(string value) => _handler.AppendLiteral(value);

      public void AppendFormatted<T>(T value) => _handler.AppendFormatted(value);

      public void AppendFormatted<T>(T value, string format) => _handler.AppendFormatted(value, format);

      public void AppendFormatted<T>(T value, int alignment) => _handler.AppendFormatted(value, alignment);

      public void AppendFormatted<T>(T value, int alignment, string format) => _handler.AppendFormatted(value, alignment, format);

      public void AppendFormatted(ReadOnlySpan<char> value) => _handler.AppendFormatted(value);

      public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);

      public void AppendFormatted(string? value) => _handler.AppendFormatted(value);

      public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);

      public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);
   }
}
