using Compze.Contracts;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

partial class Inbox
{
   partial class HandlerExecutionEngine
   {
      partial class Coordinator
      {
         ///<summary>The admission of one tessage the endpoint sends itself, from before its sender's transaction writes its row to<br/>
         /// that transaction's end. The tessage takes its place at the end of the line once its row is written, while its sender<br/>
         /// holds the lock that makes the endpoint's sends to itself commit one after another, so the line's order is their commit<br/>
         /// order. The place becomes ready to handle when the sender commits, and is withdrawn when it doesn't.</summary>
         ///<remarks>A tessage admitted once the endpoint has begun to stop takes no place in the line: its row waits for the<br/>
         /// recovery scan at the endpoint's next start, behind every tessage in the line, so the order holds.</remarks>
         internal class AdmissionOfATessageSentToItself
         {
            readonly Coordinator _coordinator;
            HandlerExecutionTask? _placeInTheLine;
            bool _hasEnded;
            bool _itsSenderCommitted;

            internal AdmissionOfATessageSentToItself(Coordinator coordinator) => _coordinator = coordinator;

            ///<summary>Takes the tessage's place at the end of the line, not yet ready to handle. Called once its row is<br/>
            /// written, before its sender's transaction ends.</summary>
            internal void TakeAPlaceInTheLine(TransportTessage.InComing tessage) => _coordinator._implementation.Update(implementation =>
            {
               State.Assert(!_itsSenderCommitted, () => $"The transaction that sent tessage {tessage.TessageId} to the endpoint itself committed before the send had ended: a send must be awaited before its unit of work ends.");
               //Its sender's transaction ended without committing, as when it timed out, and its row rolled back with it.
               if(_hasEnded) return;
               if(!implementation.TakesTessagesSentToItselfIntoTheLine) return;
               _placeInTheLine = _coordinator.NewHandlerExecutionTask(tessage, failingTime: TimeSpan.Zero, sentByTheEndpointToItself: true);
               implementation.EnqueueTessageTask(_placeInTheLine);
            });

            internal void ItsSenderCommitted() => _coordinator._implementation.Update(implementation =>
            {
               _hasEnded = true;
               _itsSenderCommitted = true;
               implementation.AdmissionOfATessageSentToItselfCommitted(_placeInTheLine);
            });

            internal void ItsSenderDidNotCommit() => _coordinator._implementation.Update(implementation =>
            {
               _hasEnded = true;
               implementation.AdmissionOfATessageSentToItselfWithdrawn(_placeInTheLine);
            });
         }
      }
   }
}
