using System.Diagnostics;
using AccountManagement.UserStories.Scenarios;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Diagnostics;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase
{
   TestingEndpointHost Host { get; }
   readonly ExactlyOnceEndpoint _endpoint;
   TypermediaTestClient _client = null!;
   internal AccountScenarioApi Scenario => new(_client.Navigator);

   //hack: flake-hunt instrumentation, never merged: a user story that takes longer than SlowUserStory fails with what the
   //   process looked like every half second while it ran, and the first one in the process dumps it.
   static readonly TimeSpan SlowUserStory = TimeSpan.FromSeconds(10);
   static int _hasDumped;
   readonly Stopwatch _sinceConstruction = Stopwatch.StartNew();
   readonly List<string> _samples = [];
   readonly ManualResetEventSlim _isDone = new();
   readonly Thread _watchdog;

   protected UserStoryTest()
   {
      _watchdog = new Thread(Watch) { IsBackground = true, Name = "flake-hunt watchdog" };
      _watchdog.Start();
      Host = TestingEndpointHost.Create();
      _endpoint = Host.RegisterEndpoint(new AccountManagementDomainEndpointDeclaration());
      Host.RegisterEndpoint(new AccountManagementStatisticsEndpointDeclaration());
   }

   void Watch()
   {
      while(!_isDone.Wait(500))
      {
         Record(Sample());
         if(_sinceConstruction.Elapsed > SlowUserStory && Interlocked.Exchange(ref _hasDumped, 1) == 0)
            Record($"{DateTime.UtcNow:HH:mm:ss.fff} dump written: {TestProcessDump.Write("flake-hunt slow user story").FullName}");
      }
   }

   void Record(string sample)
   {
      lock(_samples) _samples.Add(sample);
   }

   static string Sample()
   {
      using var process = Process.GetCurrentProcess();
      return $"{DateTime.UtcNow:HH:mm:ss.fff} pool threads {ThreadPool.ThreadCount}, pending {ThreadPool.PendingWorkItemCount}, completed {ThreadPool.CompletedWorkItemCount}, "
           + $"process threads {process.Threads.Count}, process cpu {process.TotalProcessorTime.TotalMilliseconds:F0} ms, "
           + $"gc pauses {GC.GetTotalPauseDuration().TotalMilliseconds:F0} ms, gen2 collections {GC.CollectionCount(2)}";
   }

   protected override async Task InitializeAsyncInternal()
   {
      await Host.StartAsync().caf();
      //The statistics endpoint's query models update from the domain endpoint's tevents, and exactly-once fan-out membership
      //is the remembered subscribers - first contact is the boundary - so the user stories' immediate acts await the two
      //endpoints having met instead of racing the discovery.
      await Host.AwaitEndpointsHaveMetEachOtherAsync().caf();
      _client = await TypermediaTestClient.ConnectToAsync(_endpoint.Address!, registrar => registrar.RequireAccountManagementTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      try
      {
         await _client.DisposeAsync().caf();
         await Host.DisposeAsync().caf();
      }
      finally
      {
         _isDone.Set();
         _watchdog.Join();
      }

      if(_sinceConstruction.Elapsed > SlowUserStory)
      {
         lock(_samples)
            throw new TimeoutException($"FLAKE-HUNT: the user story took {_sinceConstruction.Elapsed.TotalSeconds:F1} s. Every half second:{Environment.NewLine}{string.Join(Environment.NewLine, _samples)}");
      }
   }
}
