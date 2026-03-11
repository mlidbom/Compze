using Compze.Contracts;

namespace Compze.Threading.Testing;

public partial interface IGatedCodeSection
{
   IGatedCodeSection Open()
   {
      EntranceGate.Open();
      ExitGate.Open();
      return this;
   }

   IGatedCodeSection LetOneThreadEnterAndReachExit()
   {
      ExecuteWithExclusiveLock(it => State.Assert(it.EntranceGate.Passed == it.ExitGate.Passed, () => $"{nameof(IGatedCodeSection)} must be empty when calling this method"));
      EntranceGate.AwaitLetOneThreadPassThrough();
      ExitGate.AwaitQueueLengthEqualTo(1);
      return this;
   }

   IGatedCodeSection LetOneThreadPass()
   {
      LetOneThreadEnterAndReachExit();
      ExitGate.AwaitLetOneThreadPassThrough();
      return this;
   }

   ///<summary>Executes <paramref name="action"/> while holding the shared lock that guards both gates.</summary>
   void ExecuteWithExclusiveLock(Action<IGatedCodeSection> action) => ExecuteWithExclusiveLock(action.ToFunc());

   void Execute(Action action)
   {
      using(Enter())
      {
         action();
      }
   }
}
