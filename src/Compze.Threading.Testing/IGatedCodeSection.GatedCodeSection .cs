namespace Compze.Threading.Testing;

public partial interface IGatedCodeSection
{
   //urgent: This must share a monitor among all the parts and encapsulate more things to be able to provide synchronization guarantees. Examining, or mutating, a section under a lock must guarantee non-mutating gates.
   private class GatedCodeSection : IGatedCodeSection
   {
      public IThreadGate EntranceGate { get; }
      public IThreadGate ExitGate { get; }

      internal GatedCodeSection(WaitTimeout timeout, string name)
      {
         EntranceGate = IThreadGate.NewClosed(timeout, $"{name}.Entrance");
         ExitGate = IThreadGate.NewClosed(timeout, $"{name}.Exit");
      }

      public IDisposable Enter()
      {
         EntranceGate.AwaitPassThrough();
         return new Disposable(() => ExitGate.AwaitPassThrough());
      }
   }
}
