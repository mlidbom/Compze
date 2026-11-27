using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.Endpoints;
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

///<summary>A tessage's failing time, how long its handling has been failing, is recorded with each failed try and carries<br/>
/// across restarts: an endpoint's next process goes on from it rather than giving the tessage its whole time to retry<br/>
/// again. So a tessage whose handling had failed for all of them when the endpoint's previous process stopped is marked failed<br/>
/// at its first failed try in the next one. The previous process is scripted through the inbox sql layer: the tessage is<br/>
/// admitted, and a failed try is recorded with a failing time of the endpoint's whole time to retry.</summary>
public class Given_a_domain_database_remembering_an_inbox_tessage_retried_until_its_retry_policy_gave_up : UniversalTestBase
{
   static readonly EndpointId PreviousSendersPeerEndpointId = new(Guid.Parse("5E1C7A93-2B64-4F08-9D35-C1A7B48E260F"));
   static readonly TimeSpan TheEndpointsMaxTimeToRetry = TimeSpan.FromMinutes(1);

   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _restartedEndpoint;
   readonly IThreadGate _tommandHandlerThreadGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "TommandHandlerThreadGate");

   public Given_a_domain_database_remembering_an_inbox_tessage_retried_until_its_retry_policy_gave_up()
   {
      _host = TestingEndpointHost.Create();
      _restartedEndpoint = _host.RegisterEndpoint(new RestartedEndpointDeclaration(this));
   }

   class RestartedEndpointDeclaration : ExactlyOnceEndpointDeclaration<RestartedEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Restarted";
      public static EndpointId Id => new(Guid.Parse("C03B5D86-9E21-4A7F-B8D4-6F12E07A93C5"));

      readonly Given_a_domain_database_remembering_an_inbox_tessage_retried_until_its_retry_policy_gave_up _specification;
      internal RestartedEndpointDeclaration(Given_a_domain_database_remembering_an_inbox_tessage_retried_until_its_retry_policy_gave_up specification) => _specification = specification;

      protected override RetryPolicy? RetryPolicy => RetryPolicy.GiveUpAfter(TheEndpointsMaxTimeToRetry);

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand(async (MyExactlyOnceTommandWhoseHandlerKeepsFailing _) =>
          {
             await _specification._tommandHandlerThreadGate.AwaitPassThroughAsync();
             throw new IntentionalHandlerFailureException();
          });
   }

   protected override async Task InitializeAsyncInternal()
   {
      var inbox = _restartedEndpoint.ServiceLocator.Resolve<ITessagingSqlLayer.IInboxSqlLayer>();
      await inbox.InitAsync();

      var tommand = new MyExactlyOnceTommandWhoseHandlerKeepsFailing();
      var serializedTommand = _restartedEndpoint.ServiceLocator.Resolve<ITessagingSerializer>().SerializeTessage(tommand);
      var tommandTypeId = _restartedEndpoint.ServiceLocator.Resolve<ITypeMap>().GetId(tommand.GetType());

      (await inbox.SaveTessageAsync(tommand.Id, tommandTypeId, serializedTommand,
                                    new DeliveryStreamPosition(PreviousSendersPeerEndpointId, sequenceNumber: 1, predecessorSequenceNumber: 0)))
        .Must().Be(ITessagingSqlLayer.SaveTessageResult.NewTessage);
      (await inbox.RecordFailedTryAsync(tommand.Id, exceptionStackTrace: "", exceptionTessage: "The previous process's last failed try", exceptionType: "", failingTime: TheEndpointsMaxTimeToRetry))
        .Must().Be(1);

      await _host.StartAsync();
      await AwaitTheInboxHoldingNoUnhandledTessageAsync(inbox);
      await DisposeTheHostAsync();
   }

   ///<summary>Marking the tessage failed is what takes it out of the inbox's unhandled tessages, since its handler never<br/>
   /// succeeds. The testing host can't be awaited for it: it awaits only the tessages it saw sent, and this one was recovered.</summary>
   static async Task AwaitTheInboxHoldingNoUnhandledTessageAsync(ITessagingSqlLayer.IInboxSqlLayer inbox)
   {
      var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
      while((await inbox.GetUnHandledTessagesAsync()).Count > 0)
      {
         if(DateTime.UtcNow > deadline) throw new TimeoutException("The inbox still holds the tessage as unhandled 15 seconds after the endpoint started.");
         await Task.Delay(TimeSpan.FromMilliseconds(20));
      }
   }

   ///<summary>Disposing the host may report the handler's failure or not: the endpoint reports it just after marking the<br/>
   /// tessage failed, and the host keeps only what was reported before its endpoints stop. Whatever else it reports still fails<br/>
   /// the specification.</summary>
   async Task DisposeTheHostAsync()
   {
      //Once TestingEndpointHost collects what is reported while its endpoints stop (see the bug: comment in its DisposeAsync),
      //this can assert that the failure is reported instead of tolerating it.
      try
      {
         await _host.DisposeAsync();
      }
      catch(AggregateException exception) when(exception.Flatten().InnerExceptions.All(it => it is IntentionalHandlerFailureException)) {}
   }

   //Were the failing time not carried across the restart, the endpoint would make three tries at once, and then wait to try again.
   [PCT] public void the_endpoint_marks_the_tessage_failed_at_its_first_failed_try() => _tommandHandlerThreadGate.Passed.Must().Be(1);

   class IntentionalHandlerFailureException() : Exception("Intentional failure of the tommand's handler");
}

public class MyExactlyOnceTommandWhoseHandlerKeepsFailing : Remotable.ExactlyOnce.Tommand;
