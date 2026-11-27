using Compze.Tessaging.Validation._internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.TessageBus._private.Inbox;
using Compze.Tessaging.TessageBus._private.Outbox;
using Compze.Tessaging.TessageTypes;
using JetBrains.Annotations;

namespace Compze.Tessaging.TessageBus._private;

static class UnitOfWorkTommandSenderRegistrar
{
   public static IComponentRegistrar UnitOfWorkTommandSender(this IComponentRegistrar registrar)
      => registrar.Register(_private.UnitOfWorkTommandSender.RegisterWith);
}

///<summary>The <see cref="IUnitOfWorkTommandSender"/>: the tommand sender, which consults the endpoint's<br/>
/// <see cref="TessageHandlerRoster"/> to honor the consistency law. A tommand whose handler is in the roster executes inline,<br/>
/// through the engine's one executor, in the sender's execution — exactly-once by construction: it is one transaction and no<br/>
/// delivery machinery is involved. A tommand whose handler is in the roster and deferred is admitted to the endpoint's own<br/>
/// <see cref="IInbox"/> in the sender's transaction, which handles it in one of its own once the sender has committed. Only a<br/>
/// tommand whose handler lives elsewhere crosses the endpoint boundary, through the <see cref="IOutbox"/>'s durable<br/>
/// exactly-once delivery.</summary>
[UsedImplicitly] class UnitOfWorkTommandSender : IUnitOfWorkTommandSender
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IUnitOfWorkTommandSender>()
                                  .CreatedBy((TessageHandlerRoster roster, TessageHandlerExecutor executor, IOutbox outbox, IInbox inbox, IScopeResolver scopeResolver)
                                                => new UnitOfWorkTommandSender(roster, executor, outbox, inbox, scopeResolver)));

   readonly TessageHandlerRoster _roster;
   readonly TessageHandlerExecutor _executor;
   readonly IOutbox _outbox;
   readonly IInbox _inbox;
   readonly IScopeResolver _scopeResolver;
   readonly IUsageGuard _contextGuard;

   public UnitOfWorkTommandSender(TessageHandlerRoster roster, TessageHandlerExecutor executor, IOutbox outbox, IInbox inbox, IScopeResolver scopeResolver)
   {
      _roster = roster;
      _executor = executor;
      _outbox = outbox;
      _inbox = inbox;
      _scopeResolver = scopeResolver;
      _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(this));
   }

   public async Task SendAsync(IExactlyOnceTommand tommand, CancellationToken cancellationToken = default)
   {
      _contextGuard.EnsureAccessValid();
      //The consistency law: in-boundary is immediate and transactional, unless the handler's registration defers it. Because
      //the roster is fixed per endpoint instance, a given tommand type is always-inline or never-inline for this endpoint -
      //there is no per-send lottery.
      if(_roster.DefersTheHandlingOf(tommand.GetType()))
      {
         TessageInspector.AssertValidToExecuteLocally(tommand);
         await _inbox.AdmitTommandSentToItselfAsync(tommand).caf();
      }
      else if(_roster.HandlesTommand(tommand.GetType()))
      {
         TessageInspector.AssertValidToExecuteLocally(tommand);
         await _executor.ExecuteTommandHandlerAsync(tommand, UnitOfWorkResolver.From(_scopeResolver), cancellationToken).caf();
      }
      else
      {
         TessageInspector.AssertValidToSendRemote(tommand);
         await _outbox.SendTransactionallyAsync(tommand, cancellationToken).caf();
      }
   }
}
