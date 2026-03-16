namespace Compze.Threading.Specifications.TestInfrastructure;

abstract class CancellationTrigger : IDisposable
{
   public abstract CancellationToken Token { get; }
   public abstract void Cancel(Thread blockedThread);
   public virtual void Dispose() { }
}

class ThreadInterruptCancellationTrigger : CancellationTrigger
{
   public override CancellationToken Token => CancellationToken.None;
   public override void Cancel(Thread blockedThread) => blockedThread.Interrupt();
}

class CancellationTokenCancellationTrigger : CancellationTrigger
{
   readonly CancellationTokenSource _cts = new();

   public override CancellationToken Token => _cts.Token;
   public override void Cancel(Thread blockedThread) => _cts.Cancel();

   public override void Dispose()
   {
      _cts.Dispose();
      base.Dispose();
   }
}
