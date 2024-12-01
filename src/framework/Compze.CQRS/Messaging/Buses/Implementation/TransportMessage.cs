using System;
using Composable.Contracts;
using Composable.Refactoring.Naming;
using Composable.Serialization;

namespace Composable.Messaging.Buses.Implementation;

static class TransportMessage
{
   internal enum TransportMessageType
   {
      ExactlyOnceEvent,
      AtMostOnceCommand,
      AtMostOnceCommandWithReturnValue,
      ExactlyOnceCommand,
      NonTransactionalQuery
   }

   internal class InComing
   {
      internal readonly byte[] Client;
      internal readonly Guid MessageId;
      readonly IRemotableMessageSerializer _serializer;
      internal readonly string Body;
      internal readonly TypeId MessageTypeId;
      readonly Type _messageType;
      internal readonly TransportMessageType MessageTypeEnum;

      IMessage? _message;

      public IMessage DeserializeMessageAndCacheForNextCall()
      {
         if(_message == null)
         {
            _message = _serializer.DeserializeMessage(_messageType, Body);

            Assert.State.Assert(_message is not IExactlyOnceMessage actualMessage || MessageId == actualMessage.MessageId);
         }

         return _message;
      }

      internal InComing(string body, TypeId messageTypeId, byte[] client, Guid messageId, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
         _serializer = serializer;
         Body = body;
         MessageTypeId = messageTypeId;
         _messageType = typeMapper.GetType(messageTypeId);
         MessageTypeEnum = GetMessageTypeEnum(_messageType);
         Client = client;
         MessageId = messageId;
      }

      static TransportMessageType GetMessageTypeEnum(Type messageType)
      {
         if(typeof(IRemotableQuery<object>).IsAssignableFrom(messageType))
            return TransportMessageType.NonTransactionalQuery;
         if(typeof(IAtMostOnceCommand<object>).IsAssignableFrom(messageType))
            return TransportMessageType.AtMostOnceCommandWithReturnValue;
         if(typeof(IAtMostOnceHypermediaCommand).IsAssignableFrom(messageType))
            return TransportMessageType.AtMostOnceCommand;
         else if(typeof(IExactlyOnceEvent).IsAssignableFrom(messageType))
            return TransportMessageType.ExactlyOnceEvent;
         if(typeof(IExactlyOnceCommand).IsAssignableFrom(messageType))
            return TransportMessageType.ExactlyOnceCommand;
         else
            throw new ArgumentOutOfRangeException();
      }
   }

   internal class OutGoing
   {
      public bool IsExactlyOnceDeliveryMessage { get; }
      public readonly Guid Id;

      internal readonly TypeId Type;
      internal readonly string Body;

      public static OutGoing Create(IRemotableMessage message, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
         var messageId = (message as IAtMostOnceMessage)?.MessageId ?? Guid.NewGuid();
         var body = serializer.SerializeMessage(message);
         return new OutGoing(typeMapper.GetId(message.GetType()), messageId, body, message is IExactlyOnceMessage);
      }

      OutGoing(TypeId type, Guid id, string body, bool isExactlyOnceDeliveryMessage)
      {
         IsExactlyOnceDeliveryMessage = isExactlyOnceDeliveryMessage;
         Type = type;
         Id = id;
         Body = body;
      }
   }
}
