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
      State.Assert(EntranceGate.Passed == ExitGate.Passed, () => $"{nameof(IGatedCodeSection)} must be empty when calling this method");
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

   void Execute(Action action)
   {
      using(Enter())
      {
         action();
      }
   }
}
