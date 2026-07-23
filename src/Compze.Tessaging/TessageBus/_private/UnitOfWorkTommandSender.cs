using Compze.Tessaging.Validation._internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.Engine.HandlerRegistration;
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
/// delivery machinery is involved. Only a tommand whose handler lives elsewhere crosses the endpoint boundary, through the<br/>
/// <see cref="IOutbox"/>'s durable exactly-once delivery.</summary>
[UsedImplicitly] class UnitOfWorkTommandSender : IUnitOfWorkTommandSender
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IUnitOfWorkTommandSender>()
                                  .CreatedBy((TessageHandlerRoster roster, TessageHandlerExecutor executor, IOutbox outbox, IScopeResolver scopeResolver)
                                                => new UnitOfWorkTommandSender(roster, executor, outbox, scopeResolver)));

   readonly TessageHandlerRoster _roster;
   readonly TessageHandlerExecutor _executor;
   readonly IOutbox _outbox;
   readonly IScopeResolver _scopeResolver;
   readonly IUsageGuard _contextGuard;

   public UnitOfWorkTommandSender(TessageHandlerRoster roster, TessageHandlerExecutor executor, IOutbox outbox, IScopeResolver scopeResolver)
   {
      _roster = roster;
      _executor = executor;
      _outbox = outbox;
      _scopeResolver = scopeResolver;
      _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(this));
   }

   public async Task SendAsync(IExactlyOnceTommand tommand)
   {
      _contextGuard.EnsureAccessValid();
      //The consistency law: in-boundary is immediate and transactional. Because the roster is fixed per endpoint instance,
      //a given tommand type is always-inline or never-inline for this endpoint - there is no per-send lottery.
      if(_roster.HandlesTommand(tommand.GetType()))
      {
         TessageInspector.AssertValidToExecuteLocally(tommand);
         await _executor.ExecuteTommandHandler(tommand, UnitOfWorkResolver.From(_scopeResolver)).caf();
      }
      else
      {
         TessageInspector.AssertValidToSendRemote(tommand);
         await _outbox.SendTransactionallyAsync(tommand).caf();
      }
   }
}
