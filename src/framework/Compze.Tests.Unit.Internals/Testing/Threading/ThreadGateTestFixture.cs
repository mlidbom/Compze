using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.SystemCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing.Threading;

namespace Compze.Tests.Unit.Internals.Testing.Threading;

class ThreadGateTestFixture : IDisposable
{
   public readonly IThreadGate Gate;
   public int NumberOfThreads { get; private set; }
   IReadOnlyList<Entrant> _entrantEvents = [];
   Task[] _tasksPassingGate = [];

   class Entrant
   {
      public ManualResetEventSlim HasStarted { get; init; } = new();
      public ManualResetEventSlim HasCompleted { get; init; } = new();
   }

   public static ThreadGateTestFixture StartEntrantsOnThreads(int threadCount)
   {
      var fixture = new ThreadGateTestFixture
                    {
                       NumberOfThreads = threadCount
                    };
      fixture.StartThreads();
      return fixture;
   }

   ThreadGateTestFixture()
   {
      Gate = ThreadGate.CreateClosedWithTimeout(1.Seconds());
      NumberOfThreads = 10;
   }

   void StartThreads()
   {
      _entrantEvents = 1.Through(NumberOfThreads)
                       .Select(_ => new Entrant())
                       .ToList();

      _tasksPassingGate = _entrantEvents.Select(
                                           entrantEvent => TaskCE.RunPrioritized(
                                              () =>
                                              {
                                                 entrantEvent.HasStarted.Set();
                                                 Gate.AwaitPassThrough();
                                                 entrantEvent.HasCompleted.Set();
                                              }))
                                       .ToArray();
   }

   public int ThreadsPassedTheGate(TimeSpan waitTime)
   {
      Thread.Sleep(waitTime);
      return _entrantEvents.Count(entrant => entrant.HasCompleted.IsSet);
   }

   public ThreadGateTestFixture WaitForAllThreadsToQueueUpAtPassThrough()
   {
      Gate.Await(() => Gate.Queued == NumberOfThreads);
      return this;
   }

   public void Dispose()
   {
      Gate.Open();
      Task.WaitAll(_tasksPassingGate);
   }
}