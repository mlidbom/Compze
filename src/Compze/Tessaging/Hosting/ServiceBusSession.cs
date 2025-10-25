using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;
using System;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Typermedia.Infrastructure.Validation;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Utilities.Threading;

namespace Compze.Tessaging.Hosting;


static class ServiceBusSessionRegistrar
{
   internal static IComponentRegistrar ServiceBusSession(this IComponentRegistrar registrar)
      => registrar.Register(Hosting.ServiceBusSession.RegisterWith);
}

[UsedImplicitly] class ServiceBusSession : IServiceBusSession
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IServiceBusSession>()
                                  .CreatedBy((IOutbox outbox, CommandScheduler commandScheduler)
                                                => new ServiceBusSession(outbox, commandScheduler)));

   readonly IOutbox _transport;
   readonly CommandScheduler _commandScheduler;
   readonly IUsageGuard _contextGuard;

   public ServiceBusSession(IOutbox transport, CommandScheduler commandScheduler)
   {
      _transport = transport;
      _commandScheduler = commandScheduler;
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
      _commandScheduler.Schedule(sendAt, tommand);
   }

   void RunAssertions(IExactlyOnceTommand tommand)
   {
      _contextGuard.EnsureAccessValid();
      TessageInspector.AssertValidToSendRemote(tommand);
      CommandValidator.AssertCommandIsValid(tommand);
   }
}
