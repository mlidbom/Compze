using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using JetBrains.Annotations;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Typermedia.Infrastructure.Validation;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Internals.SystemCE.UsageGuards;

namespace Compze.Tessaging.Hosting;

static class ServiceBusSessionRegistrar
{
   public static IComponentRegistrar ServiceBusSession(this IComponentRegistrar registrar)
      => registrar.Register(Hosting.ServiceBusSession.RegisterWith);
}

[UsedImplicitly] class ServiceBusSession : IServiceBusSession
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IServiceBusSession>()
                                  .CreatedBy((IOutbox outbox, TommandScheduler tommandScheduler)
                                                => new ServiceBusSession(outbox, tommandScheduler)));

   readonly IOutbox _transport;
   readonly TommandScheduler _tommandScheduler;
   readonly IUsageGuard _contextGuard;

   public ServiceBusSession(IOutbox transport, TommandScheduler tommandScheduler)
   {
      _transport = transport;
      _tommandScheduler = tommandScheduler;
      _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(this));
   }

   public void Send(IExactlyOnceTommand tommand)
   {
      RunAssertions(tommand);
      _transport.SendTransactionally(tommand);
   }

   public void ScheduleSend(DateTime sendAt, IExactlyOnceTommand tommand)
   {
      RunAssertions(tommand);
      _tommandScheduler.Schedule(sendAt, tommand);
   }

   void RunAssertions(IExactlyOnceTommand tommand)
   {
      _contextGuard.EnsureAccessValid();
      TessageInspector.AssertValidToSendRemote(tommand);
      TommandValidator.AssertTommandIsValid(tommand);
   }
}
