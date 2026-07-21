using System.Diagnostics;
using System.Globalization;
using System.Text;
using Compze.Internals.Logging._private;

namespace Compze.Internals.Logging;

public class ConsoleLogger : Logger
{
   readonly Type _type;
   ConsoleLogger(Type type) => _type = type;
   ConsoleLogger(Type type, LogLevel level) : base(level) => _type = type;

   public static ILogger Create(Type type) => new ConsoleLogger(type);
   public override ILogger WithLogLevel(LogLevel level) => new ConsoleLogger(_type, level);

   ///<summary>Renders the ambient <see cref="Activity.Current"/> as a " {Activity=Name}" suffix so the activity is visible in console output (Serilog renders it through the output template instead).</summary>
   static string ContextSuffix() => Activity.Current is {} activity ? $" {{Activity={activity.OperationName}}}" : "";

   protected override void CriticalInternal(Exception? exception, string template, object?[]? values, string caller) =>
      Console.WriteLine(exception == null
                           ? $"{DateTime.Now:HH:mm:ss.fff} CRT {LogSourceFormatter.Format(_type.Name, caller)} ### {Render(template, values)}{ContextSuffix()}"
                           : $"{DateTime.Now:HH:mm:ss.fff} CRT {LogSourceFormatter.Format(_type.Name, caller)} ### {Render(template, values)}{ContextSuffix()}, \n: Exception: {exception}");

   protected override void ErrorInternal(Exception exception, string template, object?[]? values, string caller) =>
      Console.WriteLine(ExceptionTessageBuilder.BuildExceptionLogTessage(exception, _type, caller, Render(template, values)) + ContextSuffix());

   protected override void WarningInternal(Exception? exception, string template, object?[]? values, string caller) =>
      Console.WriteLine(exception == null
                           ? $"{DateTime.Now:HH:mm:ss.fff} WRN {LogSourceFormatter.Format(_type.Name, caller)} ### {Render(template, values)}{ContextSuffix()}"
                           : $"{DateTime.Now:HH:mm:ss.fff} WRN {LogSourceFormatter.Format(_type.Name, caller)} ### {Render(template, values)}{ContextSuffix()}, \n: Exception: {exception}");

   protected override void InfoInternal(string template, object?[]? values, string caller) =>
      Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} INF {LogSourceFormatter.Format(_type.Name, caller)} ### {Render(template, values)}{ContextSuffix()}");

   protected override void DebugInternal(string template, object?[]? values, string caller) =>
      Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} DBG {LogSourceFormatter.Format(_type.Name, caller)} ### {Render(template, values)}{ContextSuffix()}");

   protected override void TraceInternal(string template, object?[]? values, string caller) =>
      Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} TRC {LogSourceFormatter.Format(_type.Name, caller)} ### {Render(template, values)}{ContextSuffix()}");

   // Renders a Serilog-style template (with {Name} / {Name:format} / {Name,align} holes) by substituting positional values.
   // Values are bound to holes in left-to-right order, the same order the handler appended them.
   static string Render(string template, object?[]? values)
   {
      if(values == null || values.Length == 0) return template;

      var output = StringBuilderPool.Rent(template.Length + values.Length * 16);
      var holeIndex = 0;
      var i = 0;
      while(i < template.Length)
      {
         var c = template[i];
         if(c == '{')
         {
            if(i + 1 < template.Length && template[i + 1] == '{') { output.Append('{'); i += 2; continue; }
            var holeEnd = template.IndexOf('}', i + 1);
            if(holeEnd < 0) { output.Append(c); i++; continue; }
            AppendHoleValue(output, template.AsSpan(i + 1, holeEnd - i - 1), holeIndex < values.Length ? values[holeIndex] : null);
            holeIndex++;
            i = holeEnd + 1;
         } else if(c == '}' && i + 1 < template.Length && template[i + 1] == '}')
         {
            output.Append('}');
            i += 2;
         } else
         {
            output.Append(c);
            i++;
         }
      }
      return StringBuilderPool.ToStringAndReturn(output);
   }

   static void AppendHoleValue(StringBuilder output, ReadOnlySpan<char> holeSpec, object? value)
   {
      // holeSpec looks like "Name", "Name,10", "Name:F1", or "Name,10:F1". We only need the format and alignment to render the value.
      var alignment = 0;
      string? format = null;

      var colonIdx = holeSpec.IndexOf(':');
      var commaIdx = holeSpec.IndexOf(',');
      if(commaIdx >= 0 && (colonIdx < 0 || commaIdx < colonIdx))
      {
         var alignEnd = colonIdx >= 0 ? colonIdx : holeSpec.Length;
         _ = int.TryParse(holeSpec[(commaIdx + 1)..alignEnd], out alignment);
      }
      if(colonIdx >= 0) format = holeSpec[(colonIdx + 1)..].ToString();

      var rendered = value switch
      {
         null                                       => "(null)",
         IFormattable f when !string.IsNullOrEmpty(format) => f.ToString(format, CultureInfo.InvariantCulture),
         IFormattable f                                    => f.ToString(format: null, CultureInfo.InvariantCulture),
         _                                                 => value.ToString() ?? ""
      };

      if(alignment == 0) output.Append(rendered);
      else if(alignment > 0) output.Append(rendered.PadLeft(alignment));
      else output.Append(rendered.PadRight(-alignment));
   }
}
