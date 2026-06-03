using System.Runtime.CompilerServices;
using Compze.Internals.Logging.Internal;

namespace Compze.Internals.Logging;

[InterpolatedStringHandler]
public ref struct InfoLogInterpolatedStringHandler
{
   LogInterpolatedStringHandlerCore _core;

   public InfoLogInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool enabled)
   {
      enabled = !CompzeLogger.LoggingSuppressed && logger.IsEnabled(LogLevel.Info);
      _core = enabled ? LogInterpolatedStringHandlerCore.CreateEnabled(literalLength, formattedCount) : default;
   }

   public readonly void AppendLiteral(string value) => _core.AppendLiteral(value);

   public void AppendFormatted<T>(T value, [CallerArgumentExpression(nameof(value))] string name = "")
      => _core.AppendFormatted(value, format: null, alignment: 0, name);

   // ReSharper disable once MethodOverloadWithOptionalParameter Compiler-only: invoked by interpolated-string lowering, which binds the format overload correctly; never called directly.
   public void AppendFormatted<T>(T value, string format, [CallerArgumentExpression(nameof(value))] string name = "")
      => _core.AppendFormatted(value, format, alignment: 0, name);

   public void AppendFormatted<T>(T value, int alignment, [CallerArgumentExpression(nameof(value))] string name = "")
      => _core.AppendFormatted(value, format: null, alignment, name);

   // ReSharper disable once MethodOverloadWithOptionalParameter Compiler-only: invoked by interpolated-string lowering, which binds the format overload correctly; never called directly.
   public void AppendFormatted<T>(T value, int alignment, string format, [CallerArgumentExpression(nameof(value))] string name = "")
      => _core.AppendFormatted(value, format, alignment, name);

   internal readonly bool Enabled => _core.Enabled;
   internal (string template, object?[] values) Build() => _core.Build();
}
