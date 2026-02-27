using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.Testing;

public class GatedCodeSection : IGatedCodeSection
{
   public IThreadGate EntranceGate { get; }
   public IThreadGate ExitGate { get; }

   public static IGatedCodeSection WithTimeout(WaitTimeout timeout) => new GatedCodeSection(timeout);

   GatedCodeSection(WaitTimeout timeout)
   {
      EntranceGate = ThreadGate.CreateClosedWithTimeout(timeout);
      ExitGate = ThreadGate.CreateClosedWithTimeout(timeout);
   }

   public IDisposable Enter()
   {
      EntranceGate.AwaitPassThrough();
      return new Disposable(() => ExitGate.AwaitPassThrough());
   }
}
