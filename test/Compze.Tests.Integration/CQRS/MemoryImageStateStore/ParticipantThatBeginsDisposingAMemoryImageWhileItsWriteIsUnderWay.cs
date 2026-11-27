using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A participant in a unit of work's transaction that, as the transaction prepares, begins disposing a memory image, on a thread of its own,<br/>
/// and lets the transaction go on to commit only half a second later: so the disposal begins while the memory image state store's write is<br/>
/// under way, prepared and not yet committed. The store prepares its write before this participant prepares, being enlisted to prepare first.</summary>
sealed class ParticipantThatBeginsDisposingAMemoryImageWhileItsWriteIsUnderWay(Action disposeTheMemoryImage) : IEnlistmentNotification
{
   static readonly TimeSpan TimeGivenTheDisposalBeforeTheCommit = TimeSpan.FromMilliseconds(500);

   readonly Action _disposeTheMemoryImage = disposeTheMemoryImage;

   ///<summary>The disposal, once this participant has begun it.</summary>
   internal Task Disposal { get; private set; } = Task.CompletedTask;

   ///<summary>Whether the disposal had ended when the half second given it had passed, before the transaction committed.</summary>
   internal bool TheDisposalEndedBeforeTheCommit { get; private set; }

   public void Prepare(PreparingEnlistment preparingEnlistment)
   {
      Disposal = TaskCE.RunOnDedicatedThreadAsync(_disposeTheMemoryImage);
      Thread.Sleep(TimeGivenTheDisposalBeforeTheCommit);
      TheDisposalEndedBeforeTheCommit = Disposal.IsCompleted;
      preparingEnlistment.Prepared();
   }

   public void Commit(Enlistment enlistment) => enlistment.Done();
   public void Rollback(Enlistment enlistment) => enlistment.Done();
   public void InDoubt(Enlistment enlistment) => enlistment.Done();
}
