using System;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Common;
using Compze.Tessaging.Common.Typermedia.Implementation;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.SystemCE.ThreadingCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting;

[UsedImplicitly] class ServiceBusSession(IOutbox transport, CommandScheduler commandScheduler) : IServiceBusSession
{
   readonly IOutbox _transport = transport;
   readonly CommandScheduler _commandScheduler = commandScheduler;
   readonly ISingleContextUseGuard _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard());

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
      _contextGuard.AssertNoContextChangeOccurred(this);
      MessageInspector.AssertValidToSendRemote(command);
      CommandValidator.AssertCommandIsValid(command);
   }
}