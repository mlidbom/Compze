using System;
using Compze.Utilities.SystemCE;

namespace Compze.Threading.Testing;

public class GatedCodeSection : IGatedCodeSection
{
   public IThreadGate EntranceGate { get; }
   public IThreadGate ExitGate { get; }

   public static IGatedCodeSection New(WaitTimeout timeout) => new GatedCodeSection(timeout);

   GatedCodeSection(WaitTimeout timeout)
   {
      EntranceGate = ThreadGate.Closed(timeout);
      ExitGate = ThreadGate.Closed(timeout);
   }

   public IDisposable Enter()
   {
      EntranceGate.AwaitPassThrough();
      return new Disposable(() => ExitGate.AwaitPassThrough());
   }
}
