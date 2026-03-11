using Compze.Contracts;

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

   ///<summary>Executes <paramref name="action"/> while holding the shared lock that guards both gates.</summary>
   void ExecuteWithExclusiveLock(Action<IGatedCodeSection> action) => ExecuteWithExclusiveLock(action.ToFunc());

   ///<summary>Passes through <see cref="EntranceGate"/>, executes <paramref name="action"/>, then passes through <see cref="ExitGate"/>.</summary>
   Unit Execute(Action action) => Execute(action.ToFunc());
}
