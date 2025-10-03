using System;
using Compze.SystemCE.ThreadingCE;
using Compze.Tessaging.Buses.Implementation;
using Compze.Tessaging.Typermedia.Implementation;
using JetBrains.Annotations;

namespace Compze.Tessaging.Buses;

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