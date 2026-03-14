using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Time.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Implementation;

static class TommandSchedulerRegistrar
{
   public static IComponentRegistrar TommandScheduler(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.TommandScheduler.RegisterWith);
}

class TommandScheduler(IOutbox transport, ITaskRunner taskRunner) : IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<TommandScheduler>()
                                     .CreatedBy((IOutbox transport, ITaskRunner taskRunner)
                                                   => new TommandScheduler(transport, taskRunner)));

   readonly IOutbox _transport = transport;
   readonly ITaskRunner _taskRunner = taskRunner;
   Timer? _scheduledTessagesTimer;
   readonly List<ScheduledTommand> _scheduledTessages = [];
   readonly IMonitor _monitor = IMonitor.New(LockTimeout.Seconds(1));

   public async Task StartAsync()
   {
      _scheduledTessagesTimer = new Timer(callback: _ => SendDueTommands(), state: null, dueTime: 0.Seconds(), period: 100.Milliseconds());
      await Task.CompletedTask.caf();
   }

   public void Schedule(DateTime sendAt, IExactlyOnceTommand tessage) => _monitor.Locked(() =>
   {
      if(UtcTimeSource.UtcNow > sendAt.ToUniversalTimeSafely())
         throw new InvalidOperationException(message: "You cannot schedule a queuedTessageInformation to be sent in the past.");

      var scheduledTommand = new ScheduledTommand(sendAt, tessage);
      //todo:Sql.
      _scheduledTessages.Add(scheduledTommand);
   });

   void SendDueTommands()
   {
      try
      {
         _monitor.Locked(() =>
         {
            var dueCommands = _scheduledTessages.RemoveWhere(HasPassedSendTime);
            if(dueCommands.Count > 0)
               this.Log().Debug($"Sending {dueCommands.Count} due scheduled tommand(s)");
            dueCommands.ForEach(Send);
         });
      }
#pragma warning disable CA1031 // Timer callback — unhandled exceptions would crash the process
      catch(Exception exception)
#pragma warning restore CA1031
      {
         this.Log().Error(exception, "Exception in timer callback");
      }
   }

   static bool HasPassedSendTime(ScheduledTommand tessage) => UtcTimeSource.UtcNow >= tessage.SendAt;

   const string SendTaskName = $"{nameof(TommandScheduler)}_Send";
   void Send(ScheduledTommand scheduledTommand)
   {
      this.Log().Debug($"Dispatching scheduled tommand {scheduledTommand.Tommand.Id} (due at {scheduledTommand.SendAt:O})");
      _taskRunner.Run(SendTaskName, () =>
      {
         TransactionScopeCe.Execute(() => _transport.SendTransactionally(scheduledTommand.Tommand));
         this.Log().Debug($"Dispatched scheduled tommand {scheduledTommand.Tommand.Id} — transaction committed");
      });
   }

   public void Dispose() => Stop();

   public void Stop() => _scheduledTessagesTimer?.Dispose();

   class ScheduledTommand(DateTime sendAt, IExactlyOnceTommand tommand)
   {
      public DateTime SendAt { get; } = sendAt.ToUniversalTimeSafely();
      public IExactlyOnceTommand Tommand { get; } = tommand;
   }
}
