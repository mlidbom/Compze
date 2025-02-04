﻿using System;
using System.Collections.Generic;
using System.Linq;
using Compze.SystemCE;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Messaging.Buses.Implementation;

class GlobalBusStateTracker : IGlobalBusStateTracker
{
   readonly IThreadShared<NonThreadSafeImplementation> _implementation = ThreadShared.WithDefaultTimeout(new NonThreadSafeImplementation());

   public IReadOnlyList<Exception> GetExceptions() => _implementation.Update(implementation => implementation.GetExceptions());

   //performance: Do we care about queries here? Could we exclude them and lessen the contention a lot?
   public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage) => _implementation.Update(implementation => implementation.SendingMessageOnTransport(transportMessage));

   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) =>
      _implementation.Await(timeoutOverride ?? 5.Seconds(), implementation => implementation.InflightMessages.Count == 0);

   public void DoneWith(Guid messageId, Exception? exception) =>
      _implementation.Update(implementation => implementation.DoneWith(messageId, exception));

   class InFlightMessage
   {
      public int RemainingReceivers { get; set; }
   }

   class NonThreadSafeImplementation
   {
      internal readonly Dictionary<Guid, InFlightMessage> InflightMessages = [];

      readonly List<Exception> _busExceptions = [];

      public IReadOnlyList<Exception> GetExceptions() => _busExceptions.ToList();

      public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage)
      {
         var inFlightMessage = InflightMessages.GetOrAdd(transportMessage.Id, () => new InFlightMessage());
         inFlightMessage.RemainingReceivers++;
      }

      public void DoneWith(Guid messageId, Exception? exception)
      {
         if(exception != null)
         {
            _busExceptions.Add(exception);
         }

         var inFlightMessage = InflightMessages[messageId];
         inFlightMessage.RemainingReceivers--;
         if(inFlightMessage.RemainingReceivers == 0)
         {
            InflightMessages.Remove(messageId);
         }
      }
   }
}

class NullOpGlobalBusStateTracker : IGlobalBusStateTracker
{
   public IReadOnlyList<Exception> GetExceptions() => [];
   public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage) { }
   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) { }
   public void DoneWith(Guid message, Exception? exception) { }
}