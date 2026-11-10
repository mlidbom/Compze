using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Compze.Internals.Logging._private;

///<summary>The backend <see cref="ILogBackend.Console"/> returns: writes each <see cref="LogLine"/> to <see cref="Console.Out"/> as rendered text.</summary>
sealed class ConsoleLogBackend : ILogBackend
{
   internal static readonly ConsoleLogBackend Instance = new();
   ConsoleLogBackend() {}

   public void Write(LogLine line) => Console.WriteLine(Rendered(line));

   ///<remarks>An error with an exception gets the multi-line layout of <see cref="ExceptionTessageBuilder"/>; every other line is one line of text, with any exception appended.</remarks>
   static string Rendered(LogLine line) =>
      line is { Level: LogLevel.Error, Exception: {} exception }
         ? ExceptionTessageBuilder.BuildExceptionLogTessage(exception, line.SourceType, line.CallerMember, RenderedMessage(line)) + ActivitySuffix()
         : $"{DateTime.Now:HH:mm:ss.fff} {LevelCode(line.Level)} {LogSourceFormatter.Format(line.SourceType.Name, line.CallerMember)} ### {RenderedMessage(line)}{ActivitySuffix()}{ExceptionSuffix(line.Exception)}";

   static string LevelCode(LogLevel level) => level switch
   {
      LogLevel.Trace    => "TRC",
      LogLevel.Debug    => "DBG",
      LogLevel.Info     => "INF",
      LogLevel.Warning  => "WRN",
      LogLevel.Error    => "ERR",
      LogLevel.Critical => "CRT",
      _                 => throw new ArgumentOutOfRangeException(nameof(level), level, "Not a LogLevel member.")
   };

   ///<summary>Renders the ambient <see cref="Activity.Current"/> as a " {Activity=Name}" suffix so the activity is visible in console output (Serilog renders it through the output template instead).</summary>
   static string ActivitySuffix() => Activity.Current is {} activity ? $" {{Activity={activity.OperationName}}}" : "";

   static string ExceptionSuffix(Exception? exception) => exception is null ? "" : $", \n: Exception: {exception}";

   ///<summary>
   /// Renders <see cref="LogLine.MessageTemplate"/> (with <c>{Name}</c> / <c>{Name:format}</c> / <c>{Name,align}</c> holes) by
   /// substituting <see cref="LogLine.TemplateValues"/> in left-to-right order, the order the holes appear, and turning doubled
   /// braces back into single ones.
   ///</summary>
   static string RenderedMessage(LogLine line)
   {
      var template = line.MessageTemplate;
      var values = line.TemplateValues;
      if(template.AsSpan().IndexOfAny('{', '}') < 0) return template;

      var output = StringBuilderPool.Rent(template.Length + values.Count * 16);
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
            AppendHoleValue(output, template.AsSpan(i + 1, holeEnd - i - 1), holeIndex < values.Count ? values[holeIndex] : null);
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
         null                                              => "(null)",
         IFormattable f when !string.IsNullOrEmpty(format) => f.ToString(format, CultureInfo.InvariantCulture),
         IFormattable f                                    => f.ToString(format: null, CultureInfo.InvariantCulture),
         _                                                 => value.ToString() ?? ""
      };

      if(alignment == 0) output.Append(rendered);
      else if(alignment > 0) output.Append(rendered.PadLeft(alignment));
      else output.Append(rendered.PadRight(-alignment));
   }
}
