using System.Text;

namespace Compze.Internals.Logging._private;

static class LogPropertyName
{
   /// <summary>
   /// Sanitize a CallerArgumentExpression value into a valid Serilog property name.
   /// Replaces invalid characters with '_'. Fast path returns the input string if already valid.
   /// </summary>
   public static string Sanitize(string raw)
   {
      if(raw.Length == 0) return "_";
      if(IsValid(raw)) return raw;

      var sanitized = new StringBuilder(raw.Length);
      foreach(var c in raw)
      {
         sanitized.Append(IsValidChar(c) ? c : '_');
      }
      return sanitized.ToString();
   }

   static bool IsValid(string s)
   {
      foreach(var c in s)
      {
         if(!IsValidChar(c)) return false;
      }
      return true;
   }

   static bool IsValidChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}
