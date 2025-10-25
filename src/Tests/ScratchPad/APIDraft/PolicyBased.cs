// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft;

public class PolicyBased
{
   interface IThreadingPolicy { } //IEnumerable<string> LocksToTake(MessagingApi.ITessage queuedTessageInformation);

   interface ITransactionPolicy { } // String TransactionToParticipateIn(MessagingApi.ITessage queuedTessageInformation)

   class OneOperationOnAnAggregateAtATime: IThreadingPolicy { }
   class OneHandlerAtATimePerTessage : IThreadingPolicy {}
   class OneTessageAtATime : IThreadingPolicy { }
   class MultipleTessagesAtATime : IThreadingPolicy { }


   class TransactionPerTessage : ITransactionPolicy { }
   class TransactionPerHandler : ITransactionPolicy { }


   class TessageHandler
   {
      public TessageHandler(params object[] configurations){}
   }

   class HandlerGroup
   {
      public HandlerGroup(params object[] parameters) {}
   }

   class Endpoint
   {
      public Endpoint(params object[] tessageHandlers) { }
   }


   enum TessageThreadingPolicy { Serialized, Parallel, SerializeAggregateAccess }
   enum HandlerInvokation { InRegistrationOrder, InParallel }
   enum TransactionBoundary { Tessage, Handler }
   enum HandlerFailurePolicy { ContinueWithOtherHandlers, StopInvokingHandlers }

   //When sending commands the sender should specify if they wish in-transaction execution so no equivalent option for commands is needed.
   //Maybe the Synchronous option here should be a completely different type of  EventHandler / EventHandler-registration?
   enum EndpointInternalEventCascadePolicy { Synchronous, Asynchronous }

   void IllustratateRegistration()
   {
      var policiesAsInterfaces = new Endpoint(

         new TessageHandler(
            new OneHandlerAtATimePerTessage(),      //Only one handler at a time can handle a specific queuedTessageInformation.
            new OneOperationOnAnAggregateAtATime()) //Only one handler at a time can handle a queuedTessageInformation about a certain aggregate.
      );

      var policiesAsEnums =
         new Endpoint("ADomainEndpoint",
                      new HandlerGroup(
                         "Command handlers",
                         TessageThreadingPolicy.Parallel,                 //Commands should be handled in parallel or we essentially single thread our entire endpoint/service.
                         TessageThreadingPolicy.SerializeAggregateAccess, //It is useless to try to execute more than one modification of the same aggregate at a time, so let's not waste resources trying.


                         HandlerInvokation.InRegistrationOrder,           //Meaningless since there can only be one command handler.
                         TransactionBoundary.Tessage,                     //Meaningless since there can only be one command handler.
                         EndpointInternalEventCascadePolicy.Asynchronous, //Invalid for commands. Overriding the default async behavior should be done by the caller of send, Be part of the bus API

                         new TessageHandler("command handler 1"),
                         new TessageHandler("command handler 2"),
                         new TessageHandler("command handler 3"),
                         new TessageHandler("command handler 4")),
                      new HandlerGroup(
                         "Query model updaters",
                         EndpointInternalEventCascadePolicy.Synchronous, //Domain query models should be immediatelly consistent if at all possible..
                         TransactionBoundary.Tessage,                    //Setting anything else together with EndpointInternalEventCascadePolicy.Synchronous would be illegal.

                         new TessageHandler("Account email query model updater")
                      ),
                      new HandlerGroup(
                         HandlerInvokation.InParallel,
                         new TessageHandler(TessageThreadingPolicy.Parallel, "Slow event handler that often receives batches of events from one aggregate. We have verified that handling tessages in parallel is safe and it is necessary for latency reasons.")
                      )
         );
   }
}