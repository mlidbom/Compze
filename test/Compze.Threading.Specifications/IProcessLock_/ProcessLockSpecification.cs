using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;

namespace Compze.Threading.Specifications.IProcessLock_;

///<summary>What the specifications of <see cref="IProcessLock"/> start from: a lock of a name no other specification, nor another run of this one,<br/>
/// uses, in a directory for the specifications' locks. The holds a specification takes through <see cref="Holding"/> are released as it ends.</summary>
public abstract class ProcessLockSpecification : UniversalTestBase
{
   ///<summary>How long a specification waits for something that should happen: only a broken implementation takes this long.</summary>
   protected static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

   ///<summary>What the specifications take the lock for, which those who wait for it are told.</summary>
   protected const string Purpose = "specifying IProcessLock";

   protected static readonly DirectoryInfo LockDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "ProcessLocks"));

   static long _locksMade;
   readonly List<IProcessLockHold> _holds = [];

   protected ProcessLockSpecification()
   {
      LockName = $"{GetType().Name}.P{Environment.ProcessId}.{Interlocked.Increment(ref _locksMade)}";
      Lock = AnotherInstanceOfTheLock();
   }

   protected string LockName { get; }

   protected IProcessLock Lock { get; }

   ///<summary>Another <see cref="IProcessLock"/> of the same name in the same directory as <see cref="Lock"/>, made as another process would make it.</summary>
   protected IProcessLock AnotherInstanceOfTheLock() => IProcessLock.New(LockName, LockDirectory);

   ///<summary>Returns <paramref name="hold"/>, to be released as the specification ends, if it isn't null.</summary>
   protected IProcessLockHold? Holding(IProcessLockHold? hold)
   {
      if(hold != null) _holds.Add(hold);
      return hold;
   }

   protected override async Task DisposeAsyncInternal()
   {
      _holds.ForEach(it => it.Dispose());
      await base.DisposeAsyncInternal();
   }
}
