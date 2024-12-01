using System;
using System.Collections.Generic;
using System.Transactions;
using Compze.Messaging.Buses.Implementation;
using Compze.SystemCE.LinqCE;

namespace Compze.Messaging;

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
            throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(IMustBeSentTransactionally).FullName} but there is no transaction.");
         case ICannotBeSentRemotelyFromWithinTransaction when Transaction.Current != null:
            throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(ICannotBeSentRemotelyFromWithinTransaction).FullName} but there is a transaction.");
         case IAtMostOnceMessage atMostOnce when atMostOnce.MessageId == Guid.Empty:
            throw new Exception($"{nameof(IAtMostOnceMessage.MessageId)} was Guid.Empty for message of type: {message.GetType().FullName}");
      }
#pragma warning restore IDE0010
   }

   internal static void AssertValidToExecuteLocally(IMessage message)
   {
      CommonAssertions(message);

      if(message is IMustBeSentTransactionally && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(IMustBeSentTransactionally).FullName} but there is no transaction.");
   }

   static void CommonAssertions(IMessage message)
   {
      MessageTypeInspector.AssertValid(message.GetType());
      if(message is ICommand command) CommandValidator.AssertCommandIsValid(command);
   }
}