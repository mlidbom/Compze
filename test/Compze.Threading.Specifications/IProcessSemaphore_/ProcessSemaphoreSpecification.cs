using System.Diagnostics;
using System.Runtime.Versioning;
using Compze.Internals.SystemCE.IOCE;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;

namespace Compze.Threading.Specifications.IProcessSemaphore_;

///<summary>What the specifications of <see cref="IProcessSemaphore"/> start from: a semaphore of <see cref="UnitsOfTheSemaphore"/> units, of a name no<br/>
/// other specification, nor another run of this one, uses, in a directory of its own. As a specification ends, the takes it began through<br/>
/// <see cref="Waiting"/> give up, the holds it took through <see cref="Holding"/> are released, and the directory is deleted: deleting it fails if a<br/>
/// take or a hold has left one of the semaphore's files open.</summary>
public abstract class ProcessSemaphoreSpecification : UniversalTestBase
{
   ///<summary>How long a specification waits for something that should happen: only a broken implementation takes this long.</summary>
   protected static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

   ///<summary>What the specifications take units for, which those who wait meanwhile are told.</summary>
   protected const string Purpose = "specifying IProcessSemaphore";

   protected const int UnitsOfTheSemaphore = 3;

   static long _semaphoresMade;
   readonly List<IProcessSemaphoreHold> _holds = [];
   readonly List<(Task<IProcessSemaphoreHold?> Take, CancellationTokenSource GivingUp)> _waitingTakes = [];

   protected ProcessSemaphoreSpecification()
   {
      SemaphoreName = $"{GetType().Name}.P{Environment.ProcessId}.{Interlocked.Increment(ref _semaphoresMade)}";
      SemaphoreDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "ProcessSemaphores", SemaphoreName));
      Semaphore = AnotherInstanceOfTheSemaphore();
   }

   protected string SemaphoreName { get; }

   protected DirectoryInfo SemaphoreDirectory { get; }

   protected IProcessSemaphore Semaphore { get; }

   ///<summary>Another <see cref="IProcessSemaphore"/> of the same name in the same directory as <see cref="Semaphore"/>, made as another process would<br/>
   /// make it, with <paramref name="units"/> units.</summary>
   protected IProcessSemaphore AnotherInstanceOfTheSemaphore(int units = UnitsOfTheSemaphore) => IProcessSemaphore.New(SemaphoreName, units, SemaphoreDirectory);

   ///<summary>Returns <paramref name="hold"/>, to be released as the specification ends, if it isn't null.</summary>
   protected IProcessSemaphoreHold? Holding(IProcessSemaphoreHold? hold)
   {
      if(hold != null) _holds.Add(hold);
      return hold;
   }

   ///<summary>Begins a <see cref="IProcessSemaphore.TryTakeAsync"/> of <paramref name="units"/> units for <paramref name="purpose"/>, which waits at<br/>
   /// most <paramref name="waitTimeout"/>, else <see cref="Patience"/>, and returns once the take is in the semaphore's queue. <see cref="GivesUp"/><br/>
   /// cancels it. It gives up as the specification ends, if it still waits, and its hold, if it took the units, is released.</summary>
   protected Task<IProcessSemaphoreHold?> Waiting(int units, string purpose, WaitTimeout? waitTimeout = null)
   {
      var givingUp = new CancellationTokenSource();
      var take = Semaphore.TryTakeAsync(units, purpose, waitTimeout ?? new WaitTimeout(Patience), givingUp.Token);
      _waitingTakes.Add((take, givingUp));
      return take;
   }

   ///<summary>Cancels <paramref name="waitingTake"/>, which <see cref="Waiting"/> began.</summary>
   protected void GivesUp(Task<IProcessSemaphoreHold?> waitingTake) => _waitingTakes.Single(it => it.Take == waitingTake).GivingUp.Cancel();

   protected override async Task DisposeAsyncInternal()
   {
      foreach(var (take, givingUp) in _waitingTakes)
      {
         await givingUp.CancelAsync();
         try
         {
            Holding(await take);
         }
         catch(OperationCanceledException) {} //Giving up, as the specification ends, is what was asked of it.
         givingUp.Dispose();
      }

      _holds.ForEach(it => it.Dispose());
      await DeleteTheSemaphoresDirectoryAsync();
      await base.DisposeAsyncInternal();
   }

   ///<summary>Deletes <see cref="SemaphoreDirectory"/>, trying again for about two seconds while a file in it is in use: on Windows a virus scanner<br/>
   /// or an indexer can open a file just written, as a holder writes in a unit's lock file. Fails, naming the processes that use each file, if one<br/>
   /// is still in use then: a take or a hold that left it open.</summary>
   async Task DeleteTheSemaphoresDirectoryAsync()
   {
      for(var attempt = 1;; attempt++)
      {
         try
         {
            SemaphoreDirectory.Delete(recursive: true);
            return;
         }
         catch(IOException exception) when(exception.HResult == SharingViolation && OperatingSystem.IsWindows())
         {
            if(attempt == AttemptsAtTheDirectory) throw new IOException($"{exception.Message} The processes using the files in {SemaphoreDirectory.FullName}: {ProcessesUsingTheFilesOfTheSemaphore()}", exception);
            await Task.Delay(TimeSpan.FromMilliseconds(100));
         }
      }
   }

   [SupportedOSPlatform("windows")]
   string ProcessesUsingTheFilesOfTheSemaphore() =>
      string.Join("; ", SemaphoreDirectory.GetFiles().Select(file => $"{file.Name}: {string.Join(", ", IdsOfTheProcessesUsingAFile.Of(file.FullName).Select(DescribeProcess))}"));

   static string DescribeProcess(int processId)
   {
      try
      {
         using var process = Process.GetProcessById(processId);
         return $"{processId} ({process.ProcessName})";
      }
      catch(ArgumentException) //It has exited since it was found using the file.
      {
         return $"{processId} (exited)";
      }
   }

   const int AttemptsAtTheDirectory = 20;
   const int SharingViolation = unchecked((int)0x80070020);
}
