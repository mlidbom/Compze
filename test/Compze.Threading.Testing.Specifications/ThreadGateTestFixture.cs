using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Threading;
using Compze.Threading.TasksCE;
using Compze.Threading.Testing;

namespace Compze.Threading.Testing.Specifications;

class ThreadGateTestFixture : IDisposable
{
   public readonly IThreadGate Gate;
   public int NumberOfThreads { get; private init; }
   IReadOnlyList<Entrant> _entrantTevents = [];
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
      Gate = ThreadGate.Closed(WaitTimeout.Seconds(30));
      NumberOfThreads = 10;
   }

   void StartThreads()
   {
      _entrantTevents = 1.Through(NumberOfThreads)
                       .Select(_ => new Entrant())
                       .ToList();

      _tasksPassingGate = _entrantTevents.Select(
                                           entrantTevent => TaskCE.Run(
                                              () =>
                                              {
                                                 entrantTevent.HasStarted.Set();
                                                 Gate.AwaitPassThrough();
                                                 entrantTevent.HasCompleted.Set();
                                              }))
                                       .ToArray();
   }

   public int ThreadsPassedTheGate(TimeSpan waitTime)
   {
      Thread.Sleep(waitTime);
      return _entrantTevents.Count(entrant => entrant.HasCompleted.IsSet);
   }

   public ThreadGateTestFixture WaitForAllThreadsToQueueUpAtPassThrough()
   {
      Gate.AwaitQueueLengthEqualTo(NumberOfThreads);
      return this;
   }

   public void Dispose()
   {
      Gate.Open();
      Task.WaitAll(_tasksPassingGate);
   }
}
