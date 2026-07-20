using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus.Internal;
using Compze.Tessaging.Internal.Routing;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Internal.EndpointCatalog;
using Compze.Tessaging.TessageBus.Internal.Inbox;
using Compze.Tessaging.TessageBus.Internal.Outbox;

namespace Compze.Tessaging.Endpoints.ExactlyOnce;

///<summary>
/// The exactly-once endpoint: the <see cref="Endpoint"/> whose TessageBus rung is exactly-once. Everything the best-effort
/// endpoint has, plus the durable vertical in the domain database it joins: the inbox (receiver dedup, transactional retry), the outbox
/// (durable rows, recovery backlog, per-peer exactly-once in-order delivery streams), durable peer memory, and the
/// tommand-sending doors (<see cref="IUnitOfWorkTommandSender"/> /
/// <see cref="IIndependentTommandSender"/>). Serves all four tessage kinds
/// unconditionally.
///
/// Composed through <see cref="Build"/>. Which machinery carries a given tessage is decided by the tessage's type and the
/// consistency law: a send whose handler is in the roster executes inline, in the sender's execution — exactly-once by
/// construction, no delivery machinery involved — and a send whose handler lives elsewhere crosses the boundary through the
/// durable vertical.
///</summary>
public class ExactlyOnceEndpoint : Endpoint
{
   readonly IInbox _inbox;
   readonly IOutbox _outbox;
   readonly EndpointProcessLease _processLease;

   ///<summary>Composes an exactly-once endpoint: runs <paramref name="build"/> over the endpoint's declaration surface<br/>
   /// (<see cref="ExactlyOnceEndpointBuilder"/>), builds the endpoint's container, and returns the endpoint, ready for its<br/>
   /// lifecycle to be driven — directly, or by the <see cref="IEndpointHost"/> that owns it<br/>
   /// (<see cref="IEndpointHost.RegisterEndpoint{TEndpoint}"/>).</summary>
   public static ExactlyOnceEndpoint Build(IContainerBuilder containerBuilder, string name, EndpointId id, Action<ExactlyOnceEndpointBuilder> build)
   {
      var builder = new ExactlyOnceEndpointBuilder(containerBuilder, new EndpointConfiguration(name, id));
      build(builder);
      return builder.Build();
   }

   internal ExactlyOnceEndpoint(IDependencyInjectionContainer container,
                                EndpointConfiguration configuration,
                                IReadOnlyList<IEndpointAddressAnnouncer> addressAnnouncers,
                                IEndpointRegistry? endpointRegistry)
      : base(container, configuration, addressAnnouncers, endpointRegistry)
   {
      _inbox = ServiceLocator.Resolve<IInbox>();
      _outbox = ServiceLocator.Resolve<IOutbox>();
      _processLease = ServiceLocator.Resolve<EndpointProcessLease>();
   }

   ///<summary>Registers the endpoint in the domain database's endpoint catalog and claims its process lease — see<br/>
   /// <see cref="EndpointProcessLease"/>: an endpoint runs in exactly one process at a time, asserted here, before anything<br/>
   /// else touches the database.</summary>
   private protected override async Task ClaimTheProcessLeaseAsync() => await _processLease.AcquireAsync().caf();

   private protected override async Task ReleaseTheProcessLeaseAsync() => await _processLease.ReleaseAsync().caf();

   ///<summary>The inbox listens and the outbox's durable storage initializes in the listening phase — before any endpoint in<br/>
   /// the host starts sending, so the sending phase's connection delivery streams can load their recovery backlogs from it.</summary>
   private protected override async Task StartTheDurableVerticalAsync() => await Task.WhenAll(_inbox.StartAsync(), _outbox.StartAsync()).caf();

   private protected override async Task StopTheDurableVerticalAsync() => await _outbox.StopAsync().caf();

   ///<summary>Waits for the inbox to finish handling every received tessage before the container is torn down —<br/>
   /// see <see cref="IInbox.AwaitAllReceivedTessagesProcessed"/>. Synchronous by nature (an in-process monitor wait), so it<br/>
   /// completes the returned task immediately.</summary>
   private protected override Task DrainTheInboxAsync()
   {
      _inbox.AwaitAllReceivedTessagesProcessed();
      return Task.CompletedTask;
   }
}
