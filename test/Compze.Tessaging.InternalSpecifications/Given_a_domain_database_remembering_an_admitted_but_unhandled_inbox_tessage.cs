using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus._internal;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging._internal.Transport;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.TypeIdentifiers;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.InternalSpecifications;

///<summary>The inbox acknowledges a tessage to its sender at admission, so a hard crash between admission and<br/>
/// handler-commit leaves a row whose handling never finished and for which no redelivery will ever come — the sender was<br/>
/// told it arrived. The endpoint's next process must keep the acknowledged-means-will-be-handled contract: its inbox<br/>
/// recovery scan re-enqueues every admitted but unhandled tessage at start, and the handler runs.</summary>
public class Given_a_domain_database_remembering_an_admitted_but_unhandled_inbox_tessage : UniversalTestBase
{
   static readonly EndpointId CrashedSendersPeerEndpointId = new(Guid.Parse("A93F5B27-1E60-4D84-B7C2-08D4F6E39A15"));

   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _rebornEndpoint;
   readonly IThreadGate _tommandHandlerThreadGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "TommandHandlerThreadGate");

   public Given_a_domain_database_remembering_an_admitted_but_unhandled_inbox_tessage()
   {
      _host = TestingEndpointHost.Create();
      _rebornEndpoint = _host.RegisterEndpoint(new RebornEndpointDeclaration(this));
   }

   class RebornEndpointDeclaration : ExactlyOnceEndpointDeclaration<RebornEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Reborn";
      public static EndpointId Id => new(Guid.Parse("6D24C9E1-8B75-4A02-9F38-52E7A1D40B96"));

      readonly Given_a_domain_database_remembering_an_admitted_but_unhandled_inbox_tessage _specification;
      internal RebornEndpointDeclaration(Given_a_domain_database_remembering_an_admitted_but_unhandled_inbox_tessage specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((MyExactlyOnceTommandAdmittedBeforeTheCrash _) =>
          {
             _specification._tommandHandlerThreadGate.AwaitPassThrough();
             return Task.CompletedTask;
          });
   }

   //The crash is scripted through the inbox sql layer: the tessage is admitted exactly as an arriving
   //delivery would be - acknowledged to its sender, registered UnHandled - and then no handler ever runs, which is
   //precisely what a process killed between admission and handler-commit leaves behind.
   protected override async Task InitializeAsyncInternal()
   {
      var inbox = _rebornEndpoint.ServiceLocator.Resolve<ITessagingSqlLayer.IInboxSqlLayer>();
      await inbox.InitAsync();

      var tommand = new MyExactlyOnceTommandAdmittedBeforeTheCrash();
      var serializedTommand = _rebornEndpoint.ServiceLocator.Resolve<ITessagingSerializer>().SerializeTessage(tommand);
      var tommandTypeId = _rebornEndpoint.ServiceLocator.Resolve<ITypeMap>().GetId(tommand.GetType());

      (await inbox.SaveTessageAsync(tommand.Id, tommandTypeId, serializedTommand,
                                    new DeliveryStreamPosition(CrashedSendersPeerEndpointId, sequenceNumber: 1, predecessorSequenceNumber: 0)))
        .Must().Be(ITessagingSqlLayer.SaveTessageResult.NewTessage);
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public async Task the_endpoints_recovery_scan_runs_the_tessages_handler_on_start()
   {
      await _host.StartAsync();
      _tommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }
}

public class MyExactlyOnceTommandAdmittedBeforeTheCrash : Remotable.ExactlyOnce.Tommand;
