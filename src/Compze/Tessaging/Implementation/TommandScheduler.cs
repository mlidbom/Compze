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

static class TommandSchedulerRegistrar
{
   internal static IComponentRegistrar TommandScheduler(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.TommandScheduler.RegisterWith);
}

class TommandScheduler(IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner) : IDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<TommandScheduler>()
                                     .CreatedBy((IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner)
                                                   => new TommandScheduler(transport, timeSource, taskRunner)));

   readonly IOutbox _transport = transport;
   readonly IUtcTimeTimeSource _timeSource = timeSource;
   readonly ITaskRunner _taskRunner = taskRunner;
   Timer? _scheduledTessagesTimer;
   readonly List<ScheduledTommand> _scheduledTessages = [];
   readonly MonitorCE _guard = MonitorCE.WithTimeout(1.Seconds());

   public async Task StartAsync()
   {
      _scheduledTessagesTimer = new Timer(callback: _ => SendDueTommands(), state: null, dueTime: 0.Seconds(), period: 100.Milliseconds());
      await Task.CompletedTask.caf();
   }

   public void Schedule(DateTime sendAt, IExactlyOnceTommand tessage) => _guard.Update(() =>
   {
      if(_timeSource.UtcNow > sendAt.ToUniversalTimeSafely())
         throw new InvalidOperationException(message: "You cannot schedule a queuedTessageInformation to be sent in the past.");

      var scheduledTommand = new ScheduledTommand(sendAt, tessage);
      //todo:Sql.
      _scheduledTessages.Add(scheduledTommand);
   });

   void SendDueTommands() => _guard.Update(() => _scheduledTessages.RemoveWhere(HasPassedSendTime).ForEach(Send));

   bool HasPassedSendTime(ScheduledTommand tessage) => _timeSource.UtcNow >= tessage.SendAt;

   const string SendTaskName = $"{nameof(TommandScheduler)}_Send";
   void Send(ScheduledTommand scheduledTommand) => _taskRunner.Run(SendTaskName, () => TransactionScopeCe.Execute(() => _transport.SendTransactionally(scheduledTommand.Tommand)));

   public void Dispose() => Stop();

   public void Stop() => _scheduledTessagesTimer?.Dispose();

   class ScheduledTommand(DateTime sendAt, IExactlyOnceTommand tommand)
   {
      public DateTime SendAt { get; } = sendAt.ToUniversalTimeSafely();
      public IExactlyOnceTommand Tommand { get; } = tommand;
   }
}
