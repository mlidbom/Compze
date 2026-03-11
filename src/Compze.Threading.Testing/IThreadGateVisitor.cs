namespace Compze.Threading.Testing;

///<summary>
/// The visitor side of an <see cref="IThreadGate"/>. The instrumented code calls <see cref="AwaitPassThrough"/> which is controlled by the gate.<br/>
/// All other methods on <see cref="IThreadGate"/> exist to either control or observe the calls to <see cref="AwaitPassThrough"/>
/// </summary>
public interface IThreadGateVisitor
{
   ///<summary>
   /// The most central method in the whole library. All the other methods on <see cref="IThreadGate"/> are about observing and/or controlling the behavior of this method.
   /// <br/>
   /// Blocks if <see cref="IThreadGate.IsOpen"/> is false and no uncompleted call to <see cref="IThreadGate.AwaitLetOneThreadPassThrough"/> is active.
   /// <br/>
   /// The gate registers information about the calling thread in <see cref="IThreadGate.Requested"/> and <see cref="IThreadGate.Queued"/> immediately and in
   /// <see cref="IThreadGate.PassedThrough"/>, <see cref="IThreadGate.Passed"/> and <see cref="IThreadGate.Queued"/> (decrementing) before the thread exits the gate.
   /// </summary>
   Unit AwaitPassThrough();
}
