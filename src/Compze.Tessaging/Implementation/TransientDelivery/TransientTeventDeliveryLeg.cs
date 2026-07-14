using System.Transactions;
using Compze.Abstractions.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Implementation.TransientDelivery;

static class TransientTeventDeliveryRegistrar
{
   public static IComponentRegistrar TransientTeventDelivery(this IComponentRegistrar registrar)
      => registrar.Register(TransientTeventDeliveryLeg.RegisterWith);
}

///<summary>The <see cref="ITransientTeventDeliveryLeg"/>: hands a published transient tevent to the connection of every remote<br/>
/// subscriber the router matches, where each connection's in-memory transient stream delivers it best-effort and in order — no<br/>
/// store, no dedup, no retry (see <c>src/Compze.Tessaging/_docs/tevent-delivery-model.md</c>).</summary>
class TransientTeventDeliveryLeg : ITransientTeventDeliveryLeg
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      //Wiring the leg into the delivery-leg set is what makes the endpoint's ITeventPublisher route transient tevents across the wire.
      => registrar.Register(Singleton.ForSet<ITransientTeventDeliveryLeg>()
                                     .CreatedBy((ITessagingRouter tessagingRouter, EndpointConfiguration configuration)
                                                   => new TransientTeventDeliveryLeg(tessagingRouter, configuration)));

   readonly ITessagingRouter _tessagingRouter;
   readonly EndpointConfiguration _configuration;

   TransientTeventDeliveryLeg(ITessagingRouter tessagingRouter, EndpointConfiguration configuration)
   {
      _tessagingRouter = tessagingRouter;
      _configuration = configuration;
   }

   public void PublishBestEffort(IPublisherIdentifyingTevent<IRemotableTevent> wrappedTevent)
   {
      var connections = _tessagingRouter.SubscriberConnectionsFor(wrappedTevent)
                                        .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                        .ToArray(); //Participation already delivered the tevent to this endpoint's own handlers - see TeventPublisher.
      if(connections.Length == 0) return;

      //One envelope identity per publish, shared by every subscriber's delivery: it carries no dedup meaning on this leg
      //(nothing is ever re-sent) and exists so in-flight tracking sees one tessage fanning out to many endpoints.
      var envelopeId = new TessageId();
      this.Log().Debug($"Publishing transient tevent {envelopeId} ({wrappedTevent.GetType().Name}) to {connections.Length} subscriber endpoint(s)");

      if(Transaction.Current is {} transaction)
         transaction.OnCommittedSuccessfully(EnqueueOnEverySubscribersConnection);
      else
         EnqueueOnEverySubscribersConnection();
      return;

      void EnqueueOnEverySubscribersConnection()
      {
         foreach(var connection in connections)
            connection.EnqueueForTransientDelivery(wrappedTevent, envelopeId);
      }
   }
}
