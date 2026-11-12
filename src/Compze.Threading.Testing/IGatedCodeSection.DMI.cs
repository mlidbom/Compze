using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Threading.Testing;

public partial interface IGatedCodeSection
{
   ///<summary>Opens both <see cref="EntranceGate"/> and <see cref="ExitGate"/>.</summary>
   IGatedCodeSection Open()
   {
      EntranceGate.Open();
      ExitGate.Open();
      return this;
   }

   ///<summary>Lets one thread pass through <see cref="EntranceGate"/> and blocks it at <see cref="ExitGate"/>. The section must be empty when called.</summary>
   IGatedCodeSection LetOneThreadEnterAndReachExit()
   {
      ExecuteWithExclusiveLock(it => State.Assert(it.EntranceGate.Passed == it.ExitGate.Passed, () => $"{nameof(IGatedCodeSection)} must be empty when calling this method"));
      EntranceGate.AwaitLetOneThreadPassThrough();
      ExitGate.AwaitQueueLengthEqualTo(1);
      return this;
   }

   ///<summary>Lets one thread pass completely through both <see cref="EntranceGate"/> and <see cref="ExitGate"/>. The section must be empty when called.</summary>
   IGatedCodeSection LetOneThreadPass()
   {
      LetOneThreadEnterAndReachExit();
      ExitGate.AwaitLetOneThreadPassThrough();
      return this;
   }

   ///<summary>The async counterpart of <see cref="LetOneThreadEnterAndReachExit"/>: lets one thread pass through <see cref="EntranceGate"/> and awaits, holding no thread,<br/>
   /// its arrival at <see cref="ExitGate"/>, where it is held. The section must be empty when called.</summary>
   async Task LetOneThreadEnterAndReachExitAsync()
   {
      ExecuteWithExclusiveLock(it => State.Assert(it.EntranceGate.Passed == it.ExitGate.Passed, () => $"{nameof(IGatedCodeSection)} must be empty when calling this method"));
      await EntranceGate.AwaitLetOneThreadPassThroughAsync().caf();
      await ExitGate.AwaitQueueLengthEqualToAsync(1).caf();
   }

   ///<summary>The async counterpart of <see cref="LetOneThreadPass"/>: lets one thread pass completely through both <see cref="EntranceGate"/> and <see cref="ExitGate"/>,<br/>
   /// holding no thread while it waits for that. The section must be empty when called.</summary>
   async Task LetOneThreadPassAsync()
   {
      await LetOneThreadEnterAndReachExitAsync().caf();
      await ExitGate.AwaitLetOneThreadPassThroughAsync().caf();
   }

   ///<summary>Executes <paramref name="action"/> while holding the shared lock that guards both gates.</summary>
   void ExecuteWithExclusiveLock(Action<IGatedCodeSection> action) => ExecuteWithExclusiveLock(action.ToFunc());

   ///<summary>Passes through <see cref="EntranceGate"/>, executes <paramref name="action"/>, then passes through <see cref="ExitGate"/>.</summary>
   Unit Execute(Action action) => Execute(action.ToFunc());

   ///<summary>The async counterpart of <see cref="Execute(Action)"/>, for async code under test: passes through <see cref="EntranceGate"/>, awaits <paramref name="asyncAction"/>,<br/>
   /// then passes through <see cref="ExitGate"/>, holding no thread while either gate keeps it waiting.</summary>
   Task ExecuteAsync(Func<Task> asyncAction) => ExecuteAsync(async () =>
   {
      await asyncAction().caf();
      return unit;
   });
}
