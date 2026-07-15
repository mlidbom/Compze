using System.Diagnostics;
using System.Text;
using Compze.Contracts;
using NCrunch.Framework;
using Compze.Tests.SameMachine.EndpointHostProcess;
//The unqualified name Program silently resolves to the entry point xUnit v3 generates into THIS test assembly, not to the endpoint host process's class.
using EndpointHostProcessProgram = Compze.Tests.SameMachine.EndpointHostProcess.Program;

namespace Compze.Tests.Integration.SameMachine;

///<summary>The specification-side handle to the separate OS process hosting an endpoint over named pipes<br/>
/// (<see cref="EndpointHostProcessProgram"/>): launches it with the Tessaging composition the specification chooses, captures its<br/>
/// console output as it is written — the process has no test runner to speak through, so a failure inside it is otherwise<br/>
/// invisible: it just never announces itself, and the conversation fails as though no handler existed — and kills it on dispose.</summary>
sealed class EndpointHostProcessHandle : IAsyncDisposable
{
   readonly Process _process;
   readonly CapturedConsoleStream _standardOutput = new();
   readonly CapturedConsoleStream _standardError = new();

   ///<summary>Launches the endpoint host process hosting <paramref name="composition"/> (one of <see cref="EndpointHostProcessProgram"/>'s<br/>
   /// composition arguments), participating in the interprocess registry named <paramref name="registryName"/> whose backing file<br/>
   /// lives in <paramref name="workDirectory"/>.</summary>
   internal static EndpointHostProcessHandle Start(string registryName, DirectoryInfo workDirectory, string composition) => new(registryName, workDirectory, composition);

   EndpointHostProcessHandle(string registryName, DirectoryInfo workDirectory, string composition)
   {
      var startInfo = new ProcessStartInfo("dotnet", $"\"{EndpointHostProcessDll()}\" {registryName} \"{workDirectory.FullName}\" {Environment.ProcessId} {composition}")
                      {
                         UseShellExecute = false,
                         CreateNoWindow = true,
                         RedirectStandardOutput = true,
                         RedirectStandardError = true
                      };
      //Under NCrunch the endpoint host process's dependencies are coverage-instrumented and call into NCrunch runtime assemblies that
      //live in this test process's base directory but not in the endpoint host process's dependency closure. The endpoint host process
      //resolves them from the directory this variable names - see Program.MakeNCrunchInstrumentedDependenciesLoadable in its project.
      if(NCrunchEnvironment.NCrunchIsResident()) startInfo.Environment[EndpointHostProcessProgram.NCrunchRuntimeAssemblyDirectoryVariableName] = AppContext.BaseDirectory;
      _process = Process.Start(startInfo)!;
      _process.OutputDataReceived += (_, line) => _standardOutput.Append(line.Data);
      _process.ErrorDataReceived += (_, line) => _standardError.Append(line.Data);
      _process.BeginOutputReadLine();
      _process.BeginErrorReadLine();
   }

   ///<summary>Every conversation failure should first call this: when the endpoint host process died, the conversation fails as<br/>
   /// though no handler existed, and only the process's exit code and console output tell the real story.</summary>
   internal void ThrowDescribingTheFailureIfTheProcessHasExited()
   {
      if(!_process.HasExited) return;
      throw new InvalidOperationException($"The endpoint host process exited prematurely with exit code {_process.ExitCode}.{Environment.NewLine}{ConsoleOutput}");
   }

   internal string ConsoleOutput => $"""
      --- endpoint host process standard output ---
      {_standardOutput}
      --- endpoint host process standard error ---
      {_standardError}
      """;

   public async ValueTask DisposeAsync()
   {
      _process.Kill(entireProcessTree: true);
      await _process.WaitForExitAsync();
      _process.Dispose();
   }

   ///<summary>Locates <c>Compze.Tests.SameMachine.EndpointHostProcess.dll</c> in that project's OWN build output — launched from this test<br/>
   /// project's output its deps.json demands assembly versions this project's output does not carry.<br/>
   /// In a normal build the projects share one output tree, so the sibling output directory is this one with the project name swapped, which<br/>
   /// stays correct across configurations and target frameworks. Under NCrunch every project builds into its own isolated workspace — no<br/>
   /// sibling exists — but the endpoint host assembly is loaded into this process from that workspace's output, which the settings in its<br/>
   /// <c>.v3.ncrunchproject</c> file make runnable, so the loaded assembly's location is the dll to launch.</summary>
   static string EndpointHostProcessDll()
   {
      if(NCrunchEnvironment.NCrunchIsResident())
      {
         var workspaceDll = typeof(TommandSentToTheEndpointHostProcess).Assembly.Location;
         var dependenciesAreMaterializedAlongsideIt = File.Exists(Path.Combine(Path.GetDirectoryName(workspaceDll)._assert().NotNull(), "Compze.Tessaging.dll"));
         return dependenciesAreMaterializedAlongsideIt
                   ? workspaceDll
                   : throw new FileNotFoundException($"The endpoint host process's NCrunch workspace output '{workspaceDll}' is missing its dependencies, so it cannot run standalone. Its .v3.ncrunchproject file must set CopyReferencedAssembliesToWorkspace to True.");
      }

      var siblingOutputDll = Path.Combine(AppContext.BaseDirectory.Replace("Compze.Tests.Integration", "Compze.Tests.SameMachine.EndpointHostProcess", StringComparison.Ordinal),
                                          "Compze.Tests.SameMachine.EndpointHostProcess.dll");
      return File.Exists(siblingOutputDll)
                ? siblingOutputDll
                : throw new FileNotFoundException($"The endpoint host process build output was not found at '{siblingOutputDll}'. Building the solution produces it.");
   }

   ///<summary>One console stream of the endpoint host process, captured as it is written so that a failure can report what the process said.</summary>
   class CapturedConsoleStream
   {
      readonly StringBuilder _capturedText = new();

      ///<summary>The final <see cref="Process.OutputDataReceived"/>/<see cref="Process.ErrorDataReceived"/> event carries null as the<br/>
      /// end-of-stream marker; everything else is one output line.</summary>
      internal void Append(string? line)
      {
         if(line is null) return;
         lock(_capturedText) _capturedText.AppendLine(line);
      }

      public override string ToString()
      {
         lock(_capturedText) return _capturedText.ToString();
      }
   }
}
