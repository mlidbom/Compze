using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.SameMachine.EndpointHostProcess;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.xUnitMatrix;
//The unqualified name Program silently resolves to the entry point xUnit v3 generates into THIS test assembly, not to the endpoint host process's class.
using EndpointHostProcessProgram = Compze.Tests.SameMachine.EndpointHostProcess.Program;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.SameMachine;

///<summary>The same-machine story end to end, across REAL process boundaries: a separate OS process hosts an endpoint over named<br/>
/// pipes, both processes discover each other through a shared <see cref="InterprocessEndpointRegistry"/>, and an exactly-once<br/>
/// tommand conversation crosses in both directions — no web stack, no database server, no configuration.</summary>
public class Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry : UniversalTestBase
{
   //The endpoint host process speaks named pipes; the conversation only makes sense when the specification's endpoint does too.
   static bool RunsOnTheNamedPipesTransport => TestEnv.Transport == Transport.NamedPipes;

   readonly DirectoryInfo _workDirectory = null!;
   readonly InterprocessEndpointRegistry _registry = null!;
   readonly EndpointHostProcessHandle _endpointHostProcess = null!;
   readonly IEndpointHost _specificationHost = null!;
   readonly ExactlyOnceEndpoint _specificationEndpoint = null!;
   readonly IThreadGate _replyTommandGate = IThreadGate.NewOpen(WaitTimeout.Seconds(1), "replyTommand");

   public Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry()
   {
      if(!RunsOnTheNamedPipesTransport) return;

      _workDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "MultiProcess", Guid.NewGuid().ToString()));
      _workDirectory.Create();
      const string registryName = "EndpointRegistry";
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(registryName, _workDirectory);

      _endpointHostProcess = EndpointHostProcessHandle.Start(registryName, _workDirectory, EndpointHostProcessProgram.ExactlyOnceTessagingComposition);

      _specificationHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                                       ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer()));
      _specificationEndpoint = _specificationHost.RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(
         container,
         "SpecificationEndpoint",
         new EndpointId(Guid.NewGuid()),
         endpoint =>
         {
            endpoint.MapTypes(mapper => mapper.MapTypesFromAssemblyContaining<TommandSentToTheEndpointHostProcess>());
            endpoint.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
            endpoint.Database(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: endpoint.Configuration.Id.ToString()));
            endpoint.ParticipateIn(_registry);
            endpoint.RegisterTessageHandlers(handle => handle.ForTommand((TommandSentBackToTheSpecificationProcess _) =>
            {
               _replyTommandGate.AwaitPassThrough();
               return Task.CompletedTask;
            }));
         }));
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
      await _endpointHostProcess.DisposeAsync();
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

   [Skip<Transport>([Transport.AspNetCore], "The endpoint host process speaks named pipes; the conversation only makes sense when the specification's endpoint does too")]
   [PCT] public void a_tommand_sent_to_the_process_is_handled_there_and_its_reply_tommand_comes_back()
   {
      //Until this process's reconciliation loop has discovered the endpoint host process - which is still starting up -
      //the handler is unknown and sending fails loud. The retry loop rides that loudness until discovery completes.
      var retryDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
      while(true)
      {
         try
         {
            _specificationEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new TommandSentToTheEndpointHostProcess());
            break;
         }
#pragma warning disable CA1031 //Retrying until discovery completes; past the deadline the exception propagates wrapped with the endpoint host process's console output.
         catch(Exception) when(DateTime.UtcNow < retryDeadline)
         {
            _endpointHostProcess.ThrowDescribingTheFailureIfTheProcessHasExited();
            Thread.Sleep(100);
         }
         catch(Exception stillUndiscoveredAtTheRetryDeadline)
#pragma warning restore CA1031
         {
            throw new InvalidOperationException($"The endpoint host process was not discovered within the retry deadline.{Environment.NewLine}{_endpointHostProcess.ConsoleOutput}", stillUndiscoveredAtTheRetryDeadline);
         }
      }

      try
      {
         _replyTommandGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(30));
      }
      catch(Exception)
      {
         _endpointHostProcess.ThrowDescribingTheFailureIfTheProcessHasExited();
         throw;
      }
   }
}
