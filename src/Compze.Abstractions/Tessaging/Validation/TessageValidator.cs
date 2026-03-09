using System.Transactions;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Tessaging.Validation;

public static class TessageValidator
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

   public class TransactionPolicyViolationException(string message) : Exception(message + TransactionPolicyRationale)
   {
      static readonly string TransactionPolicyRationale = $"""


                                                           Rationale: 
                                                           When accessing services on a bus it is very important to understand whether or not the service you are accessing will behave transactionally or not. 
                                                           If you make a mistake with regards to this you are likely to end up with intermittent bugs resulting in corrupted data. 
                                                           Such bugs are notoriously hard both to debug and to reproduce at all.

                                                           In order to minimize your risk of encountering such problems we have runtime validations detecting if your usage of transactions in combination with services make sense.

                                                           Rules: 

                                                           * Within a transaction you are not allowed send any {typeof(ICannotBeSentRemotelyFromWithinTransaction).FullName}
                                                           These tessages will not respect the transaction so if you want to send them during a transaction you have to acknowledge that they will not by suppressing the current transaction before sending them.


                                                           * There must be a transaction for you to send a {typeof(IMustBeSentTransactionally).FullName}
                                                           The whole point of these tessage types is to guarantee you exactly once delivery. Without a transaction while sending this guarantee is lost.


                                                           """;
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
