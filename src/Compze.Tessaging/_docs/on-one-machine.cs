// ReSharper disable All

#region usings
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Transport.NamedPipes;
using Compze.TypeIdentifiers.DependencyInjection;
#endregion
//todo:compze: Compze.Tessaging.Endpoints.EndpointBuilder collides with ASP.NET Core's Microsoft.AspNetCore.Builder.EndpointBuilder, which the Website project's
//   implicit usings import. Delete this alias once Compze renames its EndpointBuilder.
using EndpointBuilder = Compze.Tessaging.Endpoints.EndpointBuilder;

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Documentation.OnOneMachine;

#region shared_contracts
//In an assembly both processes reference.
public sealed class DesktopEndpoint : IEndpointIdentity
{
   public static string Name => "Desktop";
   public static EndpointId Id { get; } = new(Guid.Parse("3B8E1F42-C6D9-4A57-8E20-F1A93C5D7B64"));
}

public sealed class IndexerEndpoint : IEndpointIdentity
{
   public static string Name => "Indexer";
   public static EndpointId Id { get; } = new(Guid.Parse("C5A04D93-17E2-4B8F-A639-0D72E8B4F1C6"));
}

public interface IIndexingProgressed : IRemotableTevent
{
   int FilesIndexed { get; }
}

public class IndexingProgressed(int filesIndexed) : IIndexingProgressed
{
   public int FilesIndexed { get; private set; } = filesIndexed;
}
#endregion

#region environment
class DesktopSuiteEnvironment : IEndpointEnvironment
{
   readonly InterprocessEndpointRegistry _registry;

   public DesktopSuiteEnvironment(InterprocessEndpointRegistry registry) => _registry = registry;

   public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
      .NamedPipeEndpointTransport()
      .NewtonsoftSerializer()
      .ParticipateIn(_registry);

   //Called only for exactly-once endpoints, and this suite has none: nothing in it is stored anywhere.
   public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) =>
      throw new NotSupportedException("The desktop suite's endpoints are all best-effort endpoints.");
}
#endregion

#region desktop_process
class DesktopEndpointDeclaration : BestEffortEndpointDeclaration<DesktopEndpoint>
{
   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<IIndexingProgressed>();

   protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
      .ForTevent((IIndexingProgressed progressed) => Console.WriteLine($"{progressed.FilesIndexed} files indexed"));
}

static class DesktopProcess
{
   internal static async Task RunAsync(DirectoryInfo suiteDirectory, Task applicationExit)
   {
      using var registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal("DesktopSuite.EndpointRegistry", suiteDirectory);
      await using var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder(), new DesktopSuiteEnvironment(registry));
      host.RegisterEndpoint(new DesktopEndpointDeclaration());
      await host.StartAsync();

      await applicationExit;
   }
}
#endregion

#region indexer_process
class IndexerEndpointDeclaration : BestEffortEndpointDeclaration<IndexerEndpoint>
{
   //Tevents published before the desktop is discovered are held for it, not lost.
   protected override IReadOnlyList<EndpointId> RequiredPeers => [DesktopEndpoint.Id];

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<IIndexingProgressed>();
}

static class IndexerProcess
{
   internal static async Task RunAsync(DirectoryInfo suiteDirectory, Task applicationExit)
   {
      using var registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal("DesktopSuite.EndpointRegistry", suiteDirectory);
      await using var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder(), new DesktopSuiteEnvironment(registry));
      var indexer = host.RegisterEndpoint(new IndexerEndpointDeclaration());
      await host.StartAsync();

      var publisher = indexer.ServiceLocator.Resolve<IIndependentTeventPublisher>();
      foreach(var filesIndexed in new[] { 100, 200, 300 })
         publisher.Publish(new IndexingProgressed(filesIndexed));

      await applicationExit;
   }
}
#endregion
