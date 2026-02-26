using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.Core.Time.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Implementation;

public static class TommandSchedulerRegistrar
{
   public static IComponentRegistrar TommandScheduler(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.TommandScheduler.RegisterWith);
}

public class TommandScheduler(IOutbox transport, ITaskRunner taskRunner) : IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<TommandScheduler>()
                                     .CreatedBy((IOutbox transport, ITaskRunner taskRunner)
                                                   => new TommandScheduler(transport, taskRunner)));

   readonly IOutbox _transport = transport;
   readonly ITaskRunner _taskRunner = taskRunner;
   Timer? _scheduledTessagesTimer;
   readonly List<ScheduledTommand> _scheduledTessages = [];
   readonly IMonitorCE _monitor = IMonitorCE.WithTimeouts(1.Seconds());

   public async Task StartAsync()
   {
      _scheduledTessagesTimer = new Timer(callback: _ => SendDueTommands(), state: null, dueTime: 0.Seconds(), period: 100.Milliseconds());
      await Task.CompletedTask.caf();
   }

   public void Schedule(DateTime sendAt, IExactlyOnceTommand tessage) => _monitor.Update(() =>
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
         _monitor.Update(() => _scheduledTessages.RemoveWhere(HasPassedSendTime).ForEach(Send));
      }
#pragma warning disable CA1031 // Timer callback — unhandled exceptions would crash the process
      catch(Exception exception)
#pragma warning restore CA1031
      {
         this.Log().Error(exception, "Exception in timer callback");
      }
   }

   bool HasPassedSendTime(ScheduledTommand tessage) => UtcTimeSource.UtcNow >= tessage.SendAt;

   const string SendTaskName = $"{nameof(TommandScheduler)}_Send";
   void Send(ScheduledTommand scheduledTommand) => _taskRunner.Run(SendTaskName, () => TransactionScopeCe.Execute(() => _transport.SendTransactionally(scheduledTommand.Tommand)));

   public void Dispose() => Stop();

   public void Stop() => _scheduledTessagesTimer?.Dispose();

   public class ScheduledTommand(DateTime sendAt, IExactlyOnceTommand tommand)
   {
      public DateTime SendAt { get; } = sendAt.ToUniversalTimeSafely();
      public IExactlyOnceTommand Tommand { get; } = tommand;
   }
}
