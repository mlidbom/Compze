using System.Diagnostics;

namespace Compze.Tests.Infrastructure.XUnit;

///<summary>One explicit test of a test assembly, running in a process of its own through the xUnit runner that every xUnit v3 test assembly<br/>
/// contains. A specification starts it to have a second process do something it can't do in its own, such as hold a lock between processes, or<br/>
/// die holding one, and passes it what it needs in environment variables. Disposing it kills the process if it still runs.</summary>
///<remarks>The test is explicit, as <c>[XF(Explicit = true)]</c> makes it, so that no ordinary test run runs it, and a test run in a child process<br/>
/// can't take the whole run down with it.</remarks>
public sealed class ExplicitTestInAChildProcess : IAsyncDisposable
{
   ///<summary>The summary the xUnit runner ends its output with when the one test it ran passed.</summary>
   public const string SummaryOfOnePassingTest = "Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0";

   readonly Process _process;
   readonly Task<string> _standardOutput;
   readonly Task<string> _standardError;

   ///<summary>Starts the explicit test <paramref name="testMethod"/> of <paramref name="testClass"/> in a process of its own, with<br/>
   /// <paramref name="environmentVariables"/> added to the environment this process passes on to it.</summary>
   public static ExplicitTestInAChildProcess Start(Type testClass, string testMethod, IReadOnlyDictionary<string, string>? environmentVariables = null) =>
      new(testClass, testMethod, environmentVariables ?? new Dictionary<string, string>());

   ///<summary>Runs the explicit test <paramref name="testMethod"/> of <paramref name="testClass"/> in a process of its own, and returns everything<br/>
   /// the process wrote: the runner's summary when the process survived, and what ended it when it didn't. A process still running after<br/>
   /// <paramref name="timeout"/> is killed, and the output says so.</summary>
   public static async Task<string> RunAsync(Type testClass, string testMethod, TimeSpan timeout, IReadOnlyDictionary<string, string>? environmentVariables = null)
   {
      await using var test = Start(testClass, testMethod, environmentVariables);
      return await test.OutputOnceItExitsAsync(timeout);
   }

   ExplicitTestInAChildProcess(Type testClass, string testMethod, IReadOnlyDictionary<string, string> environmentVariables)
   {
      var startInfo = new ProcessStartInfo("dotnet")
                      {
                         UseShellExecute = false,
                         CreateNoWindow = true,
                         RedirectStandardOutput = true,
                         RedirectStandardError = true,
                         ArgumentList = { "exec", testClass.Assembly.Location, "-method", $"{testClass.FullName}.{testMethod}", "-explicit", "only", "-noLogo", "-noColor" }
                      };
      foreach(var (name, value) in environmentVariables) startInfo.Environment[name] = value;

      _process = Process.Start(startInfo)!;
      _standardOutput = _process.StandardOutput.ReadToEndAsync();
      _standardError = _process.StandardError.ReadToEndAsync();
   }

   ///<summary>The id of the process the test runs in.</summary>
   public int ProcessId => _process.Id;

   ///<summary>True once the process has exited.</summary>
   public bool HasExited => _process.HasExited;

   ///<summary>Everything the process wrote, once it has exited: the runner's summary when the process survived, and what ended it when it<br/>
   /// didn't. A process still running after <paramref name="timeout"/> is killed, and the output says so.</summary>
   public async Task<string> OutputOnceItExitsAsync(TimeSpan timeout)
   {
      using var timeoutSource = new CancellationTokenSource(timeout);
      try
      {
         await _process.WaitForExitAsync(timeoutSource.Token);
      }
      catch(OperationCanceledException) when(timeoutSource.IsCancellationRequested)
      {
         await KillAsync();
         return $"The child process was killed after running for {timeout}:{Environment.NewLine}{await Output()}";
      }

      return $"{await Output()}{Environment.NewLine}Exit code: {_process.ExitCode}";
   }

   ///<summary>Kills the process, and returns everything it wrote.</summary>
   public async Task<string> KillAndReadTheOutputAsync()
   {
      await KillAsync();
      return await Output();
   }

   ///<summary>Kills the process, and returns once it has exited: by then the operating system has closed what it held open.</summary>
   public async Task KillAsync()
   {
      if(!_process.HasExited) _process.Kill(entireProcessTree: true);
      await _process.WaitForExitAsync();
   }

   async Task<string> Output() => $"{await _standardOutput}{await _standardError}";

   public async ValueTask DisposeAsync()
   {
      await KillAsync();
      _process.Dispose();
   }
}
