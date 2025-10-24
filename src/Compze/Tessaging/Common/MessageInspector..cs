using System;
using System.Collections.Generic;
using System.Transactions;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Common.Typermedia.Implementation;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Common;

static partial class MessageInspector
{
   internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect) => eventTypesToInspect.ForEach(MessageTypeInspector.AssertValid);

   internal static void AssertValidForSubscription<TMessage>() => MessageTypeInspector.AssertValidForSubscription(typeof(TMessage));

   internal static void AssertValid<TMessage>() => MessageTypeInspector.AssertValid(typeof(TMessage));

   internal static void AssertValidToSendRemote(IMessage message)
   {
      CommonAssertions(message);

#pragma warning disable IDE0010
      switch(message)
      {
         case IStrictlyLocalMessage strictlyLocalMessage:
            throw new AttemptToSendStrictlyLocalMessageRemotelyException(strictlyLocalMessage);
         case IMustBeSentTransactionally when Transaction.Current == null:
            throw new MissingTransactionException(message);
         case ICannotBeSentRemotelyFromWithinTransaction when Transaction.Current != null:
            throw new TransactionPresentException(message);
         case IAtMostOnceMessage atMostOnce when atMostOnce.MessageId == Guid.Empty:
            throw new MissingMessageIdException(message);
      }
#pragma warning restore IDE0010
   }

   internal static void AssertValidToExecuteLocally(IMessage message)
   {
      CommonAssertions(message);

      if(message is IMustBeSentTransactionally && Transaction.Current == null)
         throw new MissingTransactionException(message);
   }

   static void CommonAssertions(IMessage message)
   {
      MessageTypeInspector.AssertValid(message.GetType());
      if(message is ICommand command) CommandValidator.AssertCommandIsValid(command);
   }
}
