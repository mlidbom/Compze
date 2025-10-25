using System;
using System.Collections.Generic;
using System.Transactions;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Typermedia.Infrastructure.Validation;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;

static partial class MessageInspector
{
   internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect) => eventTypesToInspect.ForEach(MessageTypeInspector.AssertValid);

   internal static void AssertValidForSubscription<TMessage>() => MessageTypeInspector.AssertValidForSubscription(typeof(TMessage));

   internal static void AssertValid<TMessage>() => MessageTypeInspector.AssertValid(typeof(TMessage));

   internal static void AssertValidToSendRemote(ITessage tessage)
   {
      CommonAssertions(tessage);

#pragma warning disable IDE0010
      switch(tessage)
      {
         case IStrictlyLocalMessage strictlyLocalMessage:
            throw new AttemptToSendStrictlyLocalMessageRemotelyException(strictlyLocalMessage);
         case IMustBeSentTransactionally when Transaction.Current == null:
            throw new MissingTransactionException(tessage);
         case ICannotBeSentRemotelyFromWithinTransaction when Transaction.Current != null:
            throw new TransactionPresentException(tessage);
         case IAtMostOnceTessage atMostOnce when atMostOnce.MessageId == Guid.Empty:
            throw new MissingMessageIdException(tessage);
      }
#pragma warning restore IDE0010
   }

   internal static void AssertValidToExecuteLocally(ITessage tessage)
   {
      CommonAssertions(tessage);

      if(tessage is IMustBeSentTransactionally && Transaction.Current == null)
         throw new MissingTransactionException(tessage);
   }

   static void CommonAssertions(ITessage tessage)
   {
      MessageTypeInspector.AssertValid(tessage.GetType());
      if(tessage is ITommand command) CommandValidator.AssertCommandIsValid(command);
   }
}
