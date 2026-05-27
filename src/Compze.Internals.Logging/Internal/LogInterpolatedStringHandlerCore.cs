using System.Text;

namespace Compze.Internals.Logging.Internal;

struct LogInterpolatedStringHandlerCore
{
   StringBuilder? _template;
   object?[]? _values;
   int _valueCount;
   bool _enabled;

   public static LogInterpolatedStringHandlerCore CreateEnabled(int literalLength, int formattedCount) => new()
   {
      _template = StringBuilderPool.Rent(literalLength + formattedCount * 16),
      _values = formattedCount > 0 ? new object?[formattedCount] : [],
      _valueCount = 0,
      _enabled = true
   };

   public readonly bool Enabled => _enabled;

   public readonly void AppendLiteral(string value)
   {
      var template = _template!;
      foreach(var c in value)
      {
         template.Append(c);
         if(c == '{' || c == '}') template.Append(c);
      }
   }

   public void AppendFormatted<T>(T value, string? format, int alignment, string name)
   {
      var template = _template!;
      template.Append('{').Append(LogPropertyName.Sanitize(name));
      if(alignment != 0) template.Append(',').Append(alignment);
      if(!string.IsNullOrEmpty(format)) template.Append(':').Append(format);
      template.Append('}');
      _values![_valueCount++] = value;
   }

   public (string template, object?[] values) Build()
   {
      var template = _template != null ? StringBuilderPool.ToStringAndReturn(_template) : "";
      var values = _values ?? [];
      _template = null;
      _values = null;
      return (template, values);
   }
}
