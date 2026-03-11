namespace Compze.Threading.Testing;

public interface IThreadGateVisitor
{
   ///<summary>Blocks if <see cref="IThreadGate.IsOpen"/> is false and no uncompleted call to <see cref="IThreadGate.AwaitLetOneThreadPassThrough"/> is active.
   /// The gate registers information about the calling thread in <see cref="IThreadGate.Requested"/> and <see cref="IThreadGate.Queued"/> immediately and in
   /// <see cref="IThreadGate.PassedThrough"/>, <see cref="IThreadGate.Passed"/> and <see cref="IThreadGate.Queued"/> (decrementing) before the thread exits the gate.
   /// </summary>
   Unit AwaitPassThrough();
}
