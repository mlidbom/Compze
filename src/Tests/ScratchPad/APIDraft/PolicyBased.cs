// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft;

public class PolicyBased
{
   interface IThreadingPolicy { } //IEnumerable<string> LocksToTake(MessagingApi.ITessage queuedTessageInformation);

   interface ITransactionPolicy { } // String TransactionToParticipateIn(MessagingApi.ITessage queuedTessageInformation)

   class OneOperationOnAnTaggregateAtATime: IThreadingPolicy { }
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


   enum TessageThreadingPolicy { Serialized, Parallel, SerializeTaggregateAccess }
   enum HandlerInvokation { InRegistrationOrder, InParallel }
   enum TransactionBoundary { Tessage, Handler }
   enum HandlerFailurePolicy { ContinueWithOtherHandlers, StopInvokingHandlers }

   //When sending tommands the sender should specify if they wish in-transaction execution so no equivalent option for tommands is needed.
   //Maybe the Synchronous option here should be a completely different type of  TeventHandler / TeventHandler-registration?
   enum EndpointInternalTeventCascadePolicy { Synchronous, Asynchronous }

   void IllustratateRegistration()
   {
      var policiesAsInterfaces = new Endpoint(

         new TessageHandler(
            new OneHandlerAtATimePerTessage(),      //Only one handler at a time can handle a specific queuedTessageInformation.
            new OneOperationOnAnTaggregateAtATime()) //Only one handler at a time can handle a queuedTessageInformation about a certain taggregate.
      );

      var policiesAsEnums =
         new Endpoint("ADomainEndpoint",
                      new HandlerGroup(
                         "Tommand handlers",
                         TessageThreadingPolicy.Parallel,                 //Tommands should be handled in parallel or we essentially single thread our entire endpoint/service.
                         TessageThreadingPolicy.SerializeTaggregateAccess, //It is useless to try to execute more than one modification of the same taggregate at a time, so let's not waste resources trying.


                         HandlerInvokation.InRegistrationOrder,           //Meaningless since there can only be one tommand handler.
                         TransactionBoundary.Tessage,                     //Meaningless since there can only be one tommand handler.
                         EndpointInternalTeventCascadePolicy.Asynchronous, //Invalid for tommands. Overriding the default async behavior should be done by the caller of send, Be part of the bus API

                         new TessageHandler("tommand handler 1"),
                         new TessageHandler("tommand handler 2"),
                         new TessageHandler("tommand handler 3"),
                         new TessageHandler("tommand handler 4")),
                      new HandlerGroup(
                         "Tuery model updaters",
                         EndpointInternalTeventCascadePolicy.Synchronous, //Domain tuery models should be immediatelly consistent if at all possible..
                         TransactionBoundary.Tessage,                    //Setting anything else together with EndpointInternalTeventCascadePolicy.Synchronous would be illegal.

                         new TessageHandler("Account email tuery model updater")
                      ),
                      new HandlerGroup(
                         HandlerInvokation.InParallel,
                         new TessageHandler(TessageThreadingPolicy.Parallel, "Slow tevent handler that often receives batches of tevents from one taggregate. We have verified that handling tessages in parallel is safe and it is necessary for latency reasons.")
                      )
         );
   }
}