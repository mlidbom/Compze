namespace Compze.Internals.Logging;

///<summary>
/// One line a logger hands to the <see cref="ILogBackend"/>: how severe it is, which code logged it, its message, and the
/// exception it reports, if any.
///</summary>
public sealed class LogLine
{
   internal LogLine(LogLevel level, Type sourceType, string callerMember, string messageTemplate, IReadOnlyList<object?> templateValues, Exception? exception)
   {
      Level = level;
      SourceType = sourceType;
      CallerMember = callerMember;
      MessageTemplate = messageTemplate;
      TemplateValues = templateValues;
      Exception = exception;
   }

   ///<summary>How severe the line is.</summary>
   public LogLevel Level { get; }

   ///<summary>
   /// The type the logger that logged the line was made for, by <see cref="CompzeLogger.For{T}"/>,
   /// <see cref="CompzeLogger.For(Type)"/> or <see cref="CompzeLogger.Log{T}"/>.
   ///</summary>
   public Type SourceType { get; }

   ///<summary>The name of the method or property that logged the line.</summary>
   public string CallerMember { get; }

   ///<summary>
   /// The message, as a template in the syntax Serilog uses: each <c>{name}</c> is a hole, filled by the value at the same
   /// position in <see cref="TemplateValues"/>, and a doubled brace (<c>{{</c> or <c>}}</c>) stands for one literal brace.<br/>
   /// A message logged as plain text arrives as a template with its braces doubled and no holes.
   ///</summary>
   public string MessageTemplate { get; }

   ///<summary>The values that fill the holes in <see cref="MessageTemplate"/>, in the order the holes appear.</summary>
   public IReadOnlyList<object?> TemplateValues { get; }

   ///<summary>The exception the line reports, or null when it reports none.</summary>
   public Exception? Exception { get; }
}
