using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive.MemoryImageStateStore._private;

///<summary>Keeps a reload of the memory image state store, or its writing every taggregate anew, and its units of work apart: either waits for the<br/>
/// open units of work to complete, and a unit of work that begins meanwhile waits for it to end.</summary>
///<remarks>A unit of work's session is left when its transaction completes, possibly on another thread than the one that entered it,<br/>
/// so this counts sessions, under a lock held only while counting, instead of holding a lock for the length of a unit of work.</remarks>
sealed class UnitsOfWorkAndReloadsGate
{
   static readonly WaitTimeout Timeout = WaitTimeout.Seconds(30);

   readonly IAwaitableThreadShared<GateState> _state = IAwaitableThreadShared.New(new GateState());

   internal void EnterUnitOfWork() => _state.UpdateWhen(it => !it.HoldsUnitsOfWorkBack, it => it.OpenUnitsOfWork++, timeout: Timeout);

   internal void LeaveUnitOfWork() => _state.Update(it => it.OpenUnitsOfWork--);

   ///<summary>Runs <paramref name="work"/>, such as a reload, once no unit of work is open, holding back those that begin meanwhile.</summary>
   internal TResult WhileNoUnitOfWorkIsOpen<TResult>(Func<TResult> work)
   {
      _state.UpdateWhen(it => it is { HoldsUnitsOfWorkBack: false, OpenUnitsOfWork: 0 }, it => it.HoldsUnitsOfWorkBack = true, timeout: Timeout);
      try
      {
         return work();
      }
      finally
      {
         _state.Update(it => it.HoldsUnitsOfWorkBack = false);
      }
   }

   ///<summary>As <see cref="WhileNoUnitOfWorkIsOpen{TResult}"/>, for <paramref name="work"/> that returns nothing.</summary>
   internal void WhileNoUnitOfWorkIsOpen(Action work) => WhileNoUnitOfWorkIsOpen(() =>
   {
      work();
      return true;
   });

   sealed class GateState
   {
      internal int OpenUnitsOfWork { get; set; }

      ///<summary>Whether a reload, or a write of every taggregate anew, runs, so that units of work wait.</summary>
      internal bool HoldsUnitsOfWorkBack { get; set; }
   }
}
