using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Time.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.ResourceAccess;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation;

static class CommandSchedulerRegistrar
{
   internal static IComponentRegistrar CommandScheduler(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.CommandScheduler.RegisterWith);
}

class CommandScheduler(IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner) : IDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<CommandScheduler>()
                                     .CreatedBy((IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner)
                                                   => new CommandScheduler(transport, timeSource, taskRunner)));

   readonly IOutbox _transport = transport;
   readonly IUtcTimeTimeSource _timeSource = timeSource;
   readonly ITaskRunner _taskRunner = taskRunner;
   Timer? _scheduledTessagesTimer;
   readonly List<ScheduledCommand> _scheduledTessages = [];
   readonly MonitorCE _guard = MonitorCE.WithTimeout(1.Seconds());

   public async Task StartAsync()
   {
      _scheduledTessagesTimer = new Timer(callback: _ => SendDueCommands(), state: null, dueTime: 0.Seconds(), period: 100.Milliseconds());
      await Task.CompletedTask.caf();
   }

   public void Schedule(DateTime sendAt, IExactlyOnceTommand tessage) => _guard.Update(() =>
   {
      if(_timeSource.UtcNow > sendAt.ToUniversalTimeSafely())
         throw new InvalidOperationException(message: "You cannot schedule a queuedTessageInformation to be sent in the past.");

      var scheduledCommand = new ScheduledCommand(sendAt, tessage);
      //todo:Sql.
      _scheduledTessages.Add(scheduledCommand);
   });

   void SendDueCommands() => _guard.Update(() => _scheduledTessages.RemoveWhere(HasPassedSendTime).ForEach(Send));

   bool HasPassedSendTime(ScheduledCommand tessage) => _timeSource.UtcNow >= tessage.SendAt;

   const string SendTaskName = $"{nameof(CommandScheduler)}_Send";
   void Send(ScheduledCommand scheduledCommand) => _taskRunner.Run(SendTaskName, () => TransactionScopeCe.Execute(() => _transport.SendTransactionally(scheduledCommand.Tommand)));

   public void Dispose() => Stop();

   public void Stop() => _scheduledTessagesTimer?.Dispose();

   class ScheduledCommand(DateTime sendAt, IExactlyOnceTommand tommand)
   {
      public DateTime SendAt { get; } = sendAt.ToUniversalTimeSafely();
      public IExactlyOnceTommand Tommand { get; } = tommand;
   }
}
