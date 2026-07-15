using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.SameMachine.EndpointHostProcess;
using Compze.Typermedia;
using Compze.Typermedia.Client;
using Compze.xUnitMatrix;
//The unqualified name Program silently resolves to the entry point xUnit v3 generates into THIS test assembly, not to the endpoint host process's class.
using EndpointHostProcessProgram = Compze.Tests.SameMachine.EndpointHostProcess.Program;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.SameMachine;

///<summary>The same-machine Typermedia story end to end, across REAL process boundaries: a separate OS process hosts a<br/>
/// typermedia-serving endpoint over named pipes with no database anywhere in either process, both processes discover each other<br/>
/// through a shared <see cref="InterprocessEndpointRegistry"/>, and the specification's endpoint navigates the other process's<br/>
/// typermedia through its own <see cref="IRemoteTypermediaNavigator"/> — routed by its typermedia router's live reconciliation<br/>
/// against the registry, never by a configured address.</summary>
public class Given_a_separate_process_hosting_a_typermedia_endpoint_discovered_through_a_shared_interprocess_registry : UniversalTestBase
{
   //The endpoint host process speaks named pipes; the conversation only makes sense when the specification's endpoint does too.
   static bool RunsOnTheNamedPipesTransport => TestEnv.Transport == Transport.NamedPipes;

   readonly DirectoryInfo _workDirectory = null!;
   readonly InterprocessEndpointRegistry _registry = null!;
   readonly EndpointHostProcessHandle _endpointHostProcess = null!;
   readonly ITestingEndpointHost _specificationHost = null!;
   readonly IEndpoint _specificationEndpoint = null!;

   public Given_a_separate_process_hosting_a_typermedia_endpoint_discovered_through_a_shared_interprocess_registry()
   {
      if(!RunsOnTheNamedPipesTransport) return;

      _workDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "MultiProcess", Guid.NewGuid().ToString()));
      _workDirectory.Create();
      const string registryName = "EndpointRegistry";
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(registryName, _workDirectory);

      _endpointHostProcess = EndpointHostProcessHandle.Start(registryName, _workDirectory, EndpointHostProcessProgram.DatabaselessComposition);

      _specificationHost = TestingEndpointHost.Create();
      _specificationEndpoint = _specificationHost.RegisterEndpoint(
         "SpecificationEndpoint",
         new EndpointId(Guid.NewGuid()),
         builder =>
         {
            builder.TypeMapper.MapTypesFromAssemblyContaining<TueryAskedByTheSpecificationProcess>();
            builder.Registrar.CurrentTestsEndpointTransport();
            builder.AddDistributedTypermedia().DiscoverEndpointsThrough(_registry);
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
   [PCT] public void a_tuery_executed_here_is_answered_there()
   {
      //Until this endpoint's typermedia reconciliation loop has discovered the endpoint host process - which is still starting
      //up - no route exists for the tuery's type and navigating fails loud. The retry loop rides that loudness until discovery completes.
      AnswerToTheTueryAskedByTheSpecificationProcess answer;
      var retryDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
      while(true)
      {
         try
         {
            answer = _specificationEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteInIsolatedScope(
               scope => scope.Resolve<IRemoteTypermediaNavigator>().Get(new TueryAskedByTheSpecificationProcess()));
            break;
         }
         catch(NoHandlerForTypermediaTypeException) when(DateTime.UtcNow < retryDeadline)
         {
            _endpointHostProcess.ThrowDescribingTheFailureIfTheProcessHasExited();
            Thread.Sleep(100);
         }
         catch(NoHandlerForTypermediaTypeException stillUndiscoveredAtTheRetryDeadline)
         {
            throw new InvalidOperationException($"The endpoint host process was not discovered within the retry deadline.{Environment.NewLine}{_endpointHostProcess.ConsoleOutput}", stillUndiscoveredAtTheRetryDeadline);
         }
      }

      answer.AnsweredBy.Must().Be("EndpointHostProcess");
   }
}
