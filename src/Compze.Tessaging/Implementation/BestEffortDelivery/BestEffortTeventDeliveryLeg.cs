using System.Transactions;
using Compze.Abstractions.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Implementation.BestEffortDelivery;

static class BestEffortTeventDeliveryRegistrar
{
   public static IComponentRegistrar BestEffortTeventDelivery(this IComponentRegistrar registrar)
      => registrar.Register(BestEffortTeventDeliveryLeg.RegisterWith);
}

///<summary>The <see cref="IBestEffortTeventDeliveryLeg"/>: hands a published best-effort tevent to the connection of every remote<br/>
/// subscriber the router matches, where each connection's in-memory best-effort stream delivers it best-effort and in order — no<br/>
/// store, no dedup, no retry (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>).</summary>
class BestEffortTeventDeliveryLeg : IBestEffortTeventDeliveryLeg
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      //Wiring the leg into the delivery-leg set is what makes the endpoint's IUnitOfWorkTeventPublisher route best-effort tevents across the wire.
      => registrar.Register(Singleton.ForSet<IBestEffortTeventDeliveryLeg>()
                                     .CreatedBy((ITessagingRouter tessagingRouter, EndpointConfiguration configuration)
                                                   => new BestEffortTeventDeliveryLeg(tessagingRouter, configuration)));

   readonly ITessagingRouter _tessagingRouter;
   readonly EndpointConfiguration _configuration;

   BestEffortTeventDeliveryLeg(ITessagingRouter tessagingRouter, EndpointConfiguration configuration)
   {
      _tessagingRouter = tessagingRouter;
      _configuration = configuration;
   }

   public void PublishBestEffort(IPublisherTevent<IRemotableTevent> wrappedTevent)
   {
      var connections = _tessagingRouter.SubscriberConnectionsFor(wrappedTevent)
                                        .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                        .ToArray(); //Participation already delivered the tevent to this endpoint's own handlers - see UnitOfWorkTeventPublisher.
      if(connections.Length == 0) return;

      //One envelope identity per publish, shared by every subscriber's delivery: it carries no dedup meaning on this leg
      //(nothing is ever re-sent) and exists so in-flight tracking sees one tessage fanning out to many endpoints.
      var envelopeId = new TessageId();
      this.Log().Debug($"Publishing best-effort tevent {envelopeId} ({wrappedTevent.GetType().Name}) to {connections.Length} subscriber endpoint(s)");

      //The publisher asserts the ambient transaction before routing any delivery leg, so a best-effort tevent is always published from within a unit of work: remote delivery happens on commit, never immediately.
      Transaction.Current._assert().NotNull().OnCommittedSuccessfully(EnqueueOnEverySubscribersConnection);
      return;

      void EnqueueOnEverySubscribersConnection()
      {
         foreach(var connection in connections)
            connection.EnqueueForBestEffortDelivery(wrappedTevent, envelopeId);
      }
   }
}
