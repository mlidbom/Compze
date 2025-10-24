using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Common;
using Compze.Tessaging.Common.Typermedia.Implementation;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;
using System;
using Compze.Abstractions.Tessaging.Public;
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

   public void Send(IExactlyOnceCommand command)
   {
      RunAssertions(command);
      _transport.SendTransactionally(command);
   }

   public void ScheduleSend(DateTime sendAt, IExactlyOnceCommand command)
   {
      RunAssertions(command);
      _commandScheduler.Schedule(sendAt, command);
   }

   void RunAssertions(IExactlyOnceCommand command)
   {
      _contextGuard.EnsureAccessValid();
      MessageInspector.AssertValidToSendRemote(command);
      CommandValidator.AssertCommandIsValid(command);
   }
}
