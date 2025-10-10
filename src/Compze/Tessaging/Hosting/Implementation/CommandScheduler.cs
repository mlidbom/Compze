using Compze.Abstractions.Internal.Time;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Threading.ResourceAccess;
using Compze.Threading.TasksCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Compze.Tessaging.Hosting.Implementation;

static class CommandSchedulerRegistrar
{
   internal static IDependencyRegistrar CommandScheduler(this IDependencyRegistrar registrar)
      => registrar.Register(Implementation.CommandScheduler.RegisterWith);
}

class CommandScheduler(IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner) : IDisposable
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(Singleton.For<CommandScheduler>()
                                     .CreatedBy((IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner)
                                                   => new CommandScheduler(transport, timeSource, taskRunner)));

   readonly IOutbox _transport = transport;
   readonly IUtcTimeTimeSource _timeSource = timeSource;
   readonly ITaskRunner _taskRunner = taskRunner;
   Timer? _scheduledMessagesTimer;
   readonly List<ScheduledCommand> _scheduledMessages = [];
   readonly MonitorCE _guard = MonitorCE.WithTimeout(1.Seconds());

   public async Task StartAsync()
   {
      _scheduledMessagesTimer = new Timer(callback: _ => SendDueCommands(), state: null, dueTime: 0.Seconds(), period: 100.Milliseconds());
      await Task.CompletedTask.caf();
   }

   public void Schedule(DateTime sendAt, IExactlyOnceCommand message) => _guard.Update(() =>
   {
      if(_timeSource.UtcNow > sendAt.ToUniversalTimeSafely())
         throw new InvalidOperationException(message: "You cannot schedule a queuedMessageInformation to be sent in the past.");

      var scheduledCommand = new ScheduledCommand(sendAt, message);
      //todo:Persistence.
      _scheduledMessages.Add(scheduledCommand);
   });

   void SendDueCommands() => _guard.Update(() => _scheduledMessages.RemoveWhere(HasPassedSendTime).ForEach(Send));

   bool HasPassedSendTime(ScheduledCommand message) => _timeSource.UtcNow >= message.SendAt;

   const string SendTaskName = $"{nameof(CommandScheduler)}_Send";
   void Send(ScheduledCommand scheduledCommand) => _taskRunner.RunSwallowAndLogExceptions(SendTaskName, () => TransactionScopeCe.Execute(() => _transport.SendTransactionally(scheduledCommand.Command)));

   public void Dispose() => Stop();

   public void Stop() => _scheduledMessagesTimer?.Dispose();

   class ScheduledCommand(DateTime sendAt, IExactlyOnceCommand command)
   {
      public DateTime SendAt { get; } = sendAt.ToUniversalTimeSafely();
      public IExactlyOnceCommand Command { get; } = command;
   }
}
