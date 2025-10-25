using System;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Time;
using Compze.Utilities.Contracts;

namespace Compze.Tessaging.Implementation;

static class TransportTessage
{
   internal enum TransportTessageType
   {
      ExactlyOnceEvent,
      AtMostOnceCommand,
      AtMostOnceCommandWithReturnValue,
      ExactlyOnceCommand,
      NonTransactionalTuery
   }

   internal class InComing
   {
      internal readonly byte[] Client;
      internal readonly Guid TessageId;
      readonly IRemotableTessageSerializer _serializer;
      internal readonly string Body;
      internal readonly TypeId TessageTypeId;
      readonly Type _tessageType;
      internal readonly TransportTessageType TessageTypeEnum;

      ITessage? _tessage;

      public ITessage DeserializeTessageAndCacheForNextCall()
      {
         if(_tessage == null)
         {
            _tessage = _serializer.DeserializeTessage(_tessageType, Body);

            Assert.State.Is(_tessage is not IExactlyOnceTessage actualTessage || TessageId == actualTessage.TessageId);
         }

         return _tessage;
      }

      internal InComing(string body, TypeId tessageTypeId, byte[] client, Guid tessageId, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         _serializer = serializer;
         Body = body;
         TessageTypeId = tessageTypeId;
         _tessageType = typeMapper.GetType(tessageTypeId);
         TessageTypeEnum = GetTessageTypeEnum(_tessageType);
         Client = client;
         TessageId = tessageId;
      }

      static TransportTessageType GetTessageTypeEnum(Type tessageType)
      {
         if(typeof(IRemotableTuery<object>).IsAssignableFrom(tessageType))
            return TransportTessageType.NonTransactionalTuery;
         if(typeof(IAtMostOnceTommand<object>).IsAssignableFrom(tessageType))
            return TransportTessageType.AtMostOnceCommandWithReturnValue;
         if(typeof(IAtMostOnceHypermediaTommand).IsAssignableFrom(tessageType))
            return TransportTessageType.AtMostOnceCommand;
         else if(typeof(IExactlyOnceTevent).IsAssignableFrom(tessageType))
            return TransportTessageType.ExactlyOnceEvent;
         if(typeof(IExactlyOnceTommand).IsAssignableFrom(tessageType))
            return TransportTessageType.ExactlyOnceCommand;
         else
            throw new ArgumentOutOfRangeException();
      }
   }

   internal class OutGoing
   {
      public bool IsExactlyOnceDeliveryTessage { get; }
      public readonly Guid Id;

      internal readonly TypeId Type;
      internal readonly string Body;

      public static OutGoing Create(IRemotableTessage tessage, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         var tessageId = (tessage as IAtMostOnceTessage)?.TessageId ?? Guid.CreateVersion7();
         var body = serializer.SerializeTessage(tessage);
         return new OutGoing(typeMapper.GetId(tessage.GetType()), tessageId, body, tessage is IExactlyOnceTessage);
      }

      OutGoing(TypeId type, Guid id, string body, bool isExactlyOnceDeliveryTessage)
      {
         IsExactlyOnceDeliveryTessage = isExactlyOnceDeliveryTessage;
         Type = type;
         Id = id;
         Body = body;
      }
   }
}
