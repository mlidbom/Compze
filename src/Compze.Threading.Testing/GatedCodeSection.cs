using System;
using Compze.Utilities.SystemCE;

namespace Compze.Threading.Testing;

public class GatedCodeSection : IGatedCodeSection
{
   public IThreadGate EntranceGate { get; }
   public IThreadGate ExitGate { get; }

   public static IGatedCodeSection New(WaitTimeout timeout, string name) => new GatedCodeSection(timeout, name);

   GatedCodeSection(WaitTimeout timeout, string name)
   {
      EntranceGate = ThreadGate.Closed(timeout, $"{name}.Entrance");
      ExitGate = ThreadGate.Closed(timeout, $"{name}.Exit");
   }

   public IDisposable Enter()
   {
      EntranceGate.AwaitPassThrough();
      return new Disposable(() => ExitGate.AwaitPassThrough());
   }
}
