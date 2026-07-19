using System.Transactions;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Abstractions.Validation.Exceptions;

namespace Compze.Tessaging.Abstractions.Validation;

static class TessageValidator
{
   public static void AssertValidToExecuteLocally(ITessage tessage)
   {
      CommonAssertions(tessage);

      if(tessage is IMustBeSentTransactionally && Transaction.Current == null)
         throw new MissingTransactionException(tessage);
   }

   public static void AssertValidToSendRemote(ITessage tessage)
   {
      CommonAssertions(tessage);

#pragma warning disable IDE0010
      switch(tessage)
      {
         case IStrictlyLocalTessage strictlyLocalTessage:
            throw new AttemptToSendStrictlyLocalTessageRemotelyException(strictlyLocalTessage);
         case IMustBeSentTransactionally when Transaction.Current == null:
            throw new MissingTransactionException(tessage);
         case ICannotBeSentRemotelyFromWithinTransaction when Transaction.Current != null:
            throw new TransactionPresentException(tessage);
      }
#pragma warning restore IDE0010
   }

   static void CommonAssertions(ITessage tessage)
   {
      TessageTypeInspector.AssertValid(tessage.GetType());
      if(tessage is ITommand tommand) TommandValidator.AssertTommandIsValid(tommand);
   }

   class MissingTransactionException(ITessage tessage) :
      TransactionPolicyViolationException($"{tessage.GetType().FullName} is {typeof(IMustBeSentTransactionally).FullName} but there is no transaction.");

   class TransactionPresentException(ITessage tessage) :
      TransactionPolicyViolationException($"{tessage.GetType().FullName} is {typeof(ICannotBeSentRemotelyFromWithinTransaction).FullName} but there is a transaction.");

   class AttemptToSendStrictlyLocalTessageRemotelyException(IStrictlyLocalTessage tessage) : Exception(RemoteSendOfStrictlyLocalTessageTessage(tessage))
   {
      static string RemoteSendOfStrictlyLocalTessageTessage(IStrictlyLocalTessage tessage) => $"""


                                                                                               {tessage.GetType().FullName} cannot be sent remotely because it implements {typeof(IStrictlyLocalTessage)}.

                                                                                               Rationale: 
                                                                                               {typeof(IStrictlyLocalTessage)} implementations are designed explicitly to be used locally.
                                                                                               The result of sending them off remotely is unclear to say the least and very unlikely to end up doing what you want. 

                                                                                               """;
   }
}
