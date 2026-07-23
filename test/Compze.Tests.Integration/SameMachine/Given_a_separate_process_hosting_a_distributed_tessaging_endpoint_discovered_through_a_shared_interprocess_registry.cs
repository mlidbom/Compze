using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.SameMachine.EndpointHostProcess;
using Compze.xUnitMatrix;
//The unqualified name Program silently resolves to the entry point xUnit v3 generates into THIS test assembly, not to the endpoint host process's class.
using EndpointHostProcessProgram = Compze.Tests.SameMachine.EndpointHostProcess.Program;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.Integration.SameMachine;

///<summary>The guarantee-free same-machine story end to end, across REAL process boundaries: a separate OS process hosts a<br/>
/// best-effort endpoint over named pipes, both processes discover each other through a shared<br/>
/// <see cref="InterprocessEndpointRegistry"/>, and a best-effort tevent conversation crosses in both directions — no web stack, no<br/>
/// configuration, and no database anywhere in either process: nothing is persisted, so nothing can be lost that was promised kept.<br/>
/// The specification's endpoint declares under the shared <see cref="SpecificationProcessEndpointIdentity"/>, so the endpoint<br/>
/// host process can require it by identity.</summary>
public class Given_a_separate_process_hosting_a_distributed_tessaging_endpoint_discovered_through_a_shared_interprocess_registry : UniversalTestBase
{
   //The endpoint host process speaks named pipes; the conversation only makes sense when the specification's endpoint does too.
   static bool RunsOnTheNamedPipesTransport => TestEnv.Transport == Transport.NamedPipes;

   readonly DirectoryInfo _workDirectory = null!;
   readonly InterprocessEndpointRegistry _registry = null!;
   readonly EndpointHostProcessHandle _endpointHostProcess = null!;
   readonly IEndpointHost _specificationHost = null!;
   readonly BestEffortEndpoint _specificationEndpoint = null!;
   readonly ManualResetEventSlim _replyTeventReceived = new();

   public Given_a_separate_process_hosting_a_distributed_tessaging_endpoint_discovered_through_a_shared_interprocess_registry()
   {
      if(!RunsOnTheNamedPipesTransport) return;

      _workDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "MultiProcess", Guid.NewGuid().ToString()));
      _workDirectory.Create();
      const string registryName = "EndpointRegistry";
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(registryName, _workDirectory);

      _endpointHostProcess = EndpointHostProcessHandle.Start(registryName, _workDirectory, EndpointHostProcessProgram.DatabaselessComposition);

      _specificationHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(),
                                                          new EnvironmentParticipatingInTheSharedRegistry(_registry));
      _specificationEndpoint = _specificationHost.RegisterEndpoint(new SpecificationEndpointDeclaration(this));
   }

   ///<summary>The current test's transport and serializers, plus participation in the shared interprocess registry the<br/>
   /// endpoint host process is discovered through — no database anywhere.</summary>
   class EnvironmentParticipatingInTheSharedRegistry : IEndpointEnvironment
   {
      readonly InterprocessEndpointRegistry _registry;
      internal EnvironmentParticipatingInTheSharedRegistry(InterprocessEndpointRegistry registry) => _registry = registry;

      public void DeclareOn(EndpointBuilder endpointBuilder)
      {
         endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
         endpointBuilder.Serializer(registrar => registrar.CurrentTestsSerializersIfNotClonedContainer());
         endpointBuilder.ParticipateIn(_registry);
      }

      public void DeclareDomainDatabaseOn(ExactlyOnceEndpointBuilder endpointBuilder) {}
   }

   ///<summary>Declares under the shared <see cref="SpecificationProcessEndpointIdentity"/>: the endpoint host process requires<br/>
   /// that identity, so its reply tevent is held for this endpoint and delivered on first contact.</summary>
   class SpecificationEndpointDeclaration : BestEffortEndpointDeclaration<SpecificationProcessEndpointIdentity>
   {
      readonly Given_a_separate_process_hosting_a_distributed_tessaging_endpoint_discovered_through_a_shared_interprocess_registry _specification;
      internal SpecificationEndpointDeclaration(Given_a_separate_process_hosting_a_distributed_tessaging_endpoint_discovered_through_a_shared_interprocess_registry specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireMappedTypesFromAssemblyContaining<TommandSentToTheEndpointHostProcess>();

      ///<summary>Requiring the endpoint host process's endpoint makes the outbound leg deterministic: the tevent published<br/>
      /// before either process has discovered the other is held for the required peer and delivered on first contact instead<br/>
      /// of vanishing into the discovery race.</summary>
      protected override IReadOnlyList<EndpointId> RequiredPeers => [EndpointHostProcessEndpointIdentity.Id];

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((IBestEffortTeventPublishedByTheEndpointHostProcess _) => _specification._replyTeventReceived.Set());
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
   [PCT] public void a_best_effort_tevent_published_here_before_discovery_is_handled_there_and_its_reply_tevent_comes_back()
   {
      //ONE publish, before either process has discovered the other, and no retrying: each process requires the other's endpoint,
      //so the tevent is held for the endpoint host process until first contact, and its reply is held for this endpoint the same
      //way - queue-before-first-contact makes the startup race a non-event (see src/Compze.Tessaging/dev_docs/peer-model.md).
      _specificationEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new BestEffortTeventPublishedByTheSpecificationProcess()));

      if(!_replyTeventReceived.Wait(TimeSpan.FromSeconds(30)))
      {
         _endpointHostProcess.ThrowDescribingTheFailureIfTheProcessHasExited();
         throw new InvalidOperationException($"No best-effort tevent round trip completed within the deadline.{Environment.NewLine}{_endpointHostProcess.ConsoleOutput}");
      }
   }
}
