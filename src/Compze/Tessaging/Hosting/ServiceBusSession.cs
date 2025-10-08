using System;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Common;
using Compze.Tessaging.Common.Typermedia.Implementation;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Utilities.SystemCE.ThreadingCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting;

[UsedImplicitly] class ServiceBusSession : IServiceBusSession
{
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
      _contextGuard.AssertUseValid();
      MessageInspector.AssertValidToSendRemote(command);
      CommandValidator.AssertCommandIsValid(command);
   }
}
