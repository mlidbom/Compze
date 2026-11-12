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
   public static string Run(Type testClass, string testMethod)
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

      if(!process.WaitForExit(Timeout))
      {
         process.Kill(entireProcessTree: true);
         process.WaitForExit();
         return $"The child process was killed after running for {Timeout}:{Environment.NewLine}{standardOutput.Result}{standardError.Result}";
      }

      return $"{standardOutput.Result}{standardError.Result}{Environment.NewLine}Exit code: {process.ExitCode}";
   }
}
