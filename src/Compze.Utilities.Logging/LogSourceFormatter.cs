namespace Compze.Utilities.Logging;

public static class LogSourceFormatter
{
   const int PadWidth = 45;

   /// <summary>
   /// Formats a class name and caller member into a padded log source string.
   /// Nested classes (Outer+Inner+Deep) are shortened to (Outer+).
   /// </summary>
   public static string Format(string className, string caller)
   {
      var shortClass = ShortenNestedClass(className);
      var source = string.IsNullOrEmpty(caller) ? shortClass : $"{shortClass}.{caller}";
      return source.PadRight(PadWidth);
   }

   /// <summary>
   /// Shortens nested class names: "Inbox+HandlerExecutionEngine+Coordinator" becomes "Inbox+".
   /// Non-nested names are returned as-is.
   /// </summary>
   static string ShortenNestedClass(string className)
   {
      var plusIndex = className.IndexOfOrdinal('+');
      return plusIndex >= 0 ? className[..(plusIndex + 1)] : className;
   }
}
