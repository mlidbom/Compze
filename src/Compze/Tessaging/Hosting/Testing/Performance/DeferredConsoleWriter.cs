using System;
using System.Text;
using System.Threading.Tasks;
using Compze.Utilities.Logging;
using Compze.Utilities.Threading.ResourceAccess;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Testing.Performance;

sealed class DeferredConsoleWriter : IDisposable
{
   class Buffer
   {
      internal readonly StringBuilder Content = new();
      internal bool TestSucceeded;
   }

   readonly IThreadShared<Buffer> _buffer = IThreadShared.WithDefaultTimeout(new Buffer());

   public static TResult Execute<TResult>([InstantHandle] Func<DeferredConsoleWriter, TResult> action)
   {
      using var writer = new DeferredConsoleWriter();
      var result = action(writer);
      writer.TestSucceeded();
      return result;
   }

   public static async Task<TResult> ExecuteAsync<TResult>([InstantHandle] Func<DeferredConsoleWriter, Task<TResult>> action)
   {
      using var writer = new DeferredConsoleWriter();
      var result = await action(writer);
      writer.TestSucceeded();
      return result;
   }

   public void WriteLine(string message) => _buffer.Update(buffer => buffer.Content.AppendLine(message));

   public void WriteLine() => _buffer.Update(buffer => buffer.Content.AppendLine());

   public void WriteWarningLine(string message) =>
      _buffer.Update(buffer => buffer.Content.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {message}"));

   public void WriteImportantLine(string message) =>
      _buffer.Update(buffer => buffer.Content.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"############################## {message}"));

   public void TestSucceeded() => _buffer.Update(buffer => buffer.TestSucceeded = true);

   public void Dispose()
   {
      _buffer.Update(buffer =>
      {
         if(!buffer.TestSucceeded)
         {
            ConsoleCE.WriteLine(buffer.Content.ToString());
         }
      });
   }
}
