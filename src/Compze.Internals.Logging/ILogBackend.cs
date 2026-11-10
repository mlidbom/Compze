using Compze.Internals.Logging._private;

namespace Compze.Internals.Logging;

///<summary>
/// Where Compze's loggers send the lines they log: the console, Serilog, or a backend of your own.<br/>
/// A process has one backend at a time, chosen by assigning <see cref="CompzeLogger.Backend"/>.
///</summary>
///<remarks>
/// A logger looks up <see cref="CompzeLogger.Backend"/> each time it writes a line, so it makes no difference when the logger
/// was made: one made before the host chose its backend writes to that backend once it is chosen.<br/>
/// Only lines at a level the logger has enabled, and logged outside <see cref="CompzeLogger.SuppressLoggingWhileRunningAsync"/>,
/// reach a backend.
///</remarks>
public interface ILogBackend
{
   ///<summary>
   /// Writes <paramref name="line"/>.<br/>
   /// Called on whichever thread logged the line, so an implementation must be safe to call from several threads at once.
   ///</summary>
   void Write(LogLine line);

   ///<summary>
   /// Writes each line to <see cref="System.Console.Out"/> as one line of text: time, level, the type and member that
   /// logged it, and the message with its <see cref="LogLine.TemplateValues"/> filled in.<br/>
   /// The <see cref="CompzeLogger.Backend"/> until the host assigns another.
   ///</summary>
   static ILogBackend Console => ConsoleLogBackend.Instance;
}
