using System.Diagnostics;

namespace Compze.Must.Specifications;

/// <summary>
/// Runs one explicit test of this assembly in a process of its own, through the xUnit runner every xUnit v3 test assembly
/// contains. A test that kills its process then fails the specification that ran it, instead of taking the whole test run down.
/// </summary>
static class ExplicitTestInAChildProcess
{
   static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

   /// <summary>
   /// Everything the child process wrote while running <paramref name="testMethod"/> of <paramref name="testClass"/>: the
   /// runner's summary when the process survived, and what killed it when it did not.
   /// </summary>
   public static async Task<string> RunAsync(Type testClass, string testMethod)
   {
      var startInfo = new ProcessStartInfo("dotnet")
                      {
                         RedirectStandardOutput = true,
                         RedirectStandardError = true,
                         ArgumentList = { "exec", testClass.Assembly.Location, "-method", $"{testClass.FullName}.{testMethod}", "-explicit", "only", "-noLogo", "-noColor" }
                      };

      using var process = Process.Start(startInfo)!;
      var standardOutput = process.StandardOutput.ReadToEndAsync();
      var standardError = process.StandardError.ReadToEndAsync();

      using var timeout = new CancellationTokenSource(Timeout);
      try
      {
         await process.WaitForExitAsync(timeout.Token);
      }
      catch(OperationCanceledException) when(timeout.IsCancellationRequested)
      {
         process.Kill(entireProcessTree: true);
         await process.WaitForExitAsync();
         return $"The child process was killed after running for {Timeout}:{Environment.NewLine}{await standardOutput}{await standardError}";
      }

      return $"{await standardOutput}{await standardError}{Environment.NewLine}Exit code: {process.ExitCode}";
   }
}
