using System.Diagnostics;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.SameMachine.EndpointHostProcess;
using Compze.Threading;
using Compze.Threading.Testing;
using Xunit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.SameMachine;

///<summary>The same-machine story end to end, across REAL process boundaries: a separate OS process hosts an endpoint over named<br/>
/// pipes, both processes discover each other through a shared <see cref="InterprocessEndpointRegistry"/>, and an exactly-once<br/>
/// tommand conversation crosses in both directions — no web stack, no database server, no configuration.</summary>
public class Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry : UniversalTestBase
{
   //The endpoint host process speaks named pipes; the conversation only makes sense when the specification's endpoint does too.
   bool RunsOnTheNamedPipesTransport => TestEnv.Transport == Transport.NamedPipes;

   readonly DirectoryInfo _workDirectory = null!;
   readonly InterprocessEndpointRegistry _registry = null!;
   readonly Process _endpointHostProcess = null!;
   readonly ITestingEndpointHost _specificationHost = null!;
   readonly IEndpoint _specificationEndpoint = null!;
   readonly IThreadGate _replyTommandGate = IThreadGate.NewOpen(WaitTimeout.Seconds(1), "replyTommand");

   public Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry()
   {
      if(!RunsOnTheNamedPipesTransport) return;

      _workDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "MultiProcess", Guid.NewGuid().ToString()));
      _workDirectory.Create();
      var registryName = "EndpointRegistry";
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(registryName, _workDirectory);

      //The process must run from ITS OWN build output - launched from this test project's output its deps.json demands
      //assembly versions this project's output does not carry. The sibling output directory is this one with the project
      //name swapped, which stays correct across configurations and target frameworks.
      var endpointHostProcessDll = Path.Combine(AppContext.BaseDirectory.Replace("Compze.Tests.Integration", "Compze.Tests.SameMachine.EndpointHostProcess"),
                                                "Compze.Tests.SameMachine.EndpointHostProcess.dll");
      _endpointHostProcess = Process.Start(new ProcessStartInfo("dotnet", $"\"{endpointHostProcessDll}\" {registryName} \"{_workDirectory.FullName}\" {Environment.ProcessId}")
      {
         UseShellExecute = false,
         CreateNoWindow = true
      })!;

      _specificationHost = TestingEndpointHost.Create();
      _specificationEndpoint = _specificationHost.RegisterEndpoint(
         "SpecificationEndpoint",
         new EndpointId(Guid.NewGuid()),
         builder =>
         {
            builder.TypeMapper.MapTypesFromAssemblyContaining<TommandSentToTheEndpointHostProcess>();
            builder.Registrar
                   .Register(Singleton.For<IEndpointRegistry>().Instance(_registry))
                   .CurrentTestsTessagingTransport()
                   .CurrentTestsConfiguredSqlLayer(connectionStringName: builder.Configuration.Id.ToString());
            builder.AddDistributedTessaging().AnnounceAddressTo(_registry);
            builder.RegisterTessagingHandlers.ForTommand<TommandSentBackToTheSpecificationProcess>(_ => _replyTommandGate.AwaitPassThrough());
         });
   }

   protected override async Task InitializeAsyncInternal()
   {
      if(!RunsOnTheNamedPipesTransport) return;
      await _specificationHost.StartAsync();
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(!RunsOnTheNamedPipesTransport) return;
      await _specificationHost.DisposeAsync();
      _endpointHostProcess.Kill(entireProcessTree: true);
      await _endpointHostProcess.WaitForExitAsync();
      _endpointHostProcess.Dispose();
      _registry.Delete();
      _registry.Dispose();
      await DeleteWorkDirectoryRetryingWhileTheKilledProcessesFileLocksRelease();
   }

   ///<summary>The killed process's memory-mapped file sections can outlive its exit by a moment, so the recursive delete retries briefly.<br/>
   /// Cleaning the OS temp directory is hygiene, not an assertion: if a lock outlasts the retries the directory is left for the OS.</summary>
   async Task DeleteWorkDirectoryRetryingWhileTheKilledProcessesFileLocksRelease()
   {
      for(var attempt = 1; attempt <= 20; attempt++)
      {
         try
         {
            _workDirectory.Delete(recursive: true);
            return;
         }
         catch(IOException)
         {
            await Task.Delay(100);
         }
      }
   }

   [PCT] public void a_tommand_sent_to_the_process_is_handled_there_and_its_reply_tommand_comes_back()
   {
      Assert.SkipUnless(RunsOnTheNamedPipesTransport, "The endpoint host process speaks named pipes; this conversation exists only on the NamedPipes transport.");

      //Until this process's reconciliation loop has discovered the endpoint host process - which is still starting up -
      //the handler is unknown and sending fails loud. The retry loop rides that loudness until discovery completes.
      var retryDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
      while(true)
      {
         try
         {
            _specificationEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new TommandSentToTheEndpointHostProcess()));
            break;
         }
#pragma warning disable CA1031 //Retrying until discovery completes; past the deadline the filter is false and the real exception propagates.
         catch(Exception) when(DateTime.UtcNow < retryDeadline)
#pragma warning restore CA1031
         {
            Thread.Sleep(100);
         }
      }

      _replyTommandGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(30));
   }
}
