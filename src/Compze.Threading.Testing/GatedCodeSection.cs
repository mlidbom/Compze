using System;
using Compze.Utilities.SystemCE;

namespace Compze.Threading.Testing;

//urgent: Consider whether, this should share a monitor among all the parts and encapsulate more things to be able to provide synchronization guarantees.
//possibly, for the gates themselves, it should expose only readonly interfaces and provide any mutating methods explicitly.
public class GatedCodeSection : IGatedCodeSection
{
   public IThreadGate EntranceGate { get; }
   public IThreadGate ExitGate { get; }

   public static IGatedCodeSection Closed(WaitTimeout timeout, string name) => new GatedCodeSection(timeout, name);

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
