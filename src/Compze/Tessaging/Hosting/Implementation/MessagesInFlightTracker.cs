using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.Threading.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Tessaging.Hosting.Implementation;

class MessagesInFlightTracker(ITypeMapper typeMapper) : IMessagesInFlightTracker
{
   readonly IThreadShared<NonThreadSafeImplementation> _implementation = IThreadShared.WithDefaultTimeout(new NonThreadSafeImplementation(typeMapper));

   public IReadOnlyList<Exception> GetExceptions() => _implementation.Update(implementation => implementation.GetExceptions());

   //performance: Do we care about queries here? Could we exclude them and lessen the contention a lot?
   public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage, EndpointId remoteEndpointId) =>
      _implementation.Update(implementation => implementation.SendingMessageOnTransport(transportMessage, remoteEndpointId));

   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) =>
      _implementation.Await(timeoutOverride ?? 5.Seconds(), implementation => implementation.NoMessagesInFlight());

   public void DoneWith(TransportMessage.InComing message, EndpointId handlingEndpointId, Exception? exception) =>
      _implementation.Update(implementation => implementation.DoneWith(message, handlingEndpointId, exception));

   class InFlightMessage
   {
      public Dictionary<EndpointId, bool> EndpointDeliveryStatus { get; } = [];
   }

   class NonThreadSafeImplementation(ITypeMapper typeMapper)
   {
      readonly ITypeMapper _typeMapper = typeMapper;
      internal readonly Dictionary<Guid, InFlightMessage> TrackedMessages = [];

      readonly List<Exception> _busExceptions = [];

      public IReadOnlyList<Exception> GetExceptions() => _busExceptions.ToList();

      public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage, EndpointId remoteEndpointId)
      {
         var inFlightMessage = TrackedMessages.GetOrAdd(transportMessage.Id, () => new InFlightMessage());
         inFlightMessage.EndpointDeliveryStatus[remoteEndpointId] = false;
      }

      public void DoneWith(TransportMessage.InComing message, EndpointId handlingEndpointId, Exception? exception)
      {
         var messageType = _typeMapper.GetType(message.MessageTypeId);
         if(messageType == typeof(MessageTypesInternal.EndpointInformationQuery))
            return; //this is an initial endpoint information request though which the endpoint IDs we use to track messages is first established.
         if(exception != null)
         {
            _busExceptions.Add(exception);
         }

         var inFlightMessage = TrackedMessages[message.MessageId];
         inFlightMessage.EndpointDeliveryStatus[handlingEndpointId] = true;
      }

      public bool NoMessagesInFlight() => TrackedMessages.Values.SelectMany(it => it.EndpointDeliveryStatus.Values).All(delivered => delivered);
   }
}

class NullOpMessagesInFlightTracker : IMessagesInFlightTracker
{
   public IReadOnlyList<Exception> GetExceptions() => [];
   public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage, EndpointId remoteEndpointId) {}
   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) {}
   public void DoneWith(TransportMessage.InComing message, EndpointId handlingEndpointId, Exception? exception) {}
}
