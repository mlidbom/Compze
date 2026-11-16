using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive.StateStore._private;

///<summary>Keeps a reload of the state store and its units of work apart: a reload waits for the open units of work to complete, and<br/>
/// a unit of work that begins during a reload waits for it to end.</summary>
///<remarks>A unit of work's session is left when its transaction completes, possibly on another thread than the one that entered it,<br/>
/// so this counts sessions, under a lock held only while counting, instead of holding a lock for the length of a unit of work.</remarks>
sealed class UnitsOfWorkAndReloadsGate
{
   static readonly WaitTimeout Timeout = WaitTimeout.Seconds(30);

   readonly IAwaitableThreadShared<GateState> _state = IAwaitableThreadShared.New(new GateState());

   internal void EnterUnitOfWork() => _state.UpdateWhen(it => !it.IsReloading, it => it.OpenUnitsOfWork++, timeout: Timeout);

   internal void LeaveUnitOfWork() => _state.Update(it => it.OpenUnitsOfWork--);

   internal TResult Reload<TResult>(Func<TResult> reload)
   {
      _state.UpdateWhen(it => it is { IsReloading: false, OpenUnitsOfWork: 0 }, it => it.IsReloading = true, timeout: Timeout);
      try
      {
         return reload();
      }
      finally
      {
         _state.Update(it => it.IsReloading = false);
      }
   }

   sealed class GateState
   {
      internal int OpenUnitsOfWork { get; set; }
      internal bool IsReloading { get; set; }
   }
}
