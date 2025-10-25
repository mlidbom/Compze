using System;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Time;
using Compze.Utilities.Contracts;

namespace Compze.Tessaging.Implementation;

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

      ITessage? _message;

      public ITessage DeserializeMessageAndCacheForNextCall()
      {
         if(_message == null)
         {
            _message = _serializer.DeserializeTessage(_messageType, Body);

            Assert.State.Is(_message is not IExactlyOnceTessage actualMessage || MessageId == actualMessage.MessageId);
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
         if(typeof(IRemotableTuery<object>).IsAssignableFrom(messageType))
            return TransportMessageType.NonTransactionalQuery;
         if(typeof(IAtMostOnceTommand<object>).IsAssignableFrom(messageType))
            return TransportMessageType.AtMostOnceCommandWithReturnValue;
         if(typeof(IAtMostOnceHypermediaTommand).IsAssignableFrom(messageType))
            return TransportMessageType.AtMostOnceCommand;
         else if(typeof(IExactlyOnceTevent).IsAssignableFrom(messageType))
            return TransportMessageType.ExactlyOnceEvent;
         if(typeof(IExactlyOnceTommand).IsAssignableFrom(messageType))
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

      public static OutGoing Create(IRemotableTessage tessage, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
         var messageId = (tessage as IAtMostOnceTessage)?.MessageId ?? Guid.CreateVersion7();
         var body = serializer.SerializeMessage(tessage);
         return new OutGoing(typeMapper.GetId(tessage.GetType()), messageId, body, tessage is IExactlyOnceTessage);
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
