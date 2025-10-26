using System;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.Contracts;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

static class TransportTessage
{
   internal enum TransportTessageType
   {
      ExactlyOnceTevent,
      AtMostOnceTommand,
      AtMostOnceTommandWithReturnValue,
      ExactlyOnceTommand,
      NonTransactionalTuery
   }

   internal class InComing
   {
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

      internal InComing(string body, TypeId tessageTypeId, Guid tessageId, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         _serializer = serializer;
         Body = body;
         TessageTypeId = tessageTypeId;
         _tessageType = typeMapper.GetType(tessageTypeId);
         TessageTypeEnum = GetTessageTypeEnum(_tessageType);
         TessageId = tessageId;
      }
   }

   internal class OutGoing
   {
      public ITypeMapper TypeMapper { get; }
      public IRemotableTessageSerializer Serializer { get; }
      public bool IsExactlyOnceDeliveryTessage { get; }
      public readonly Guid Id;

      internal readonly TypeId Type;
      internal readonly string Body;
      internal readonly TransportTessageType TessageTypeEnum;

      public static OutGoing Create(IRemotableTessage tessage, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         var tessageId = (tessage as IAtMostOnceTessage)?.TessageId ?? Guid.CreateVersion7();
         var body = serializer.SerializeTessage(tessage);
         return new OutGoing(typeMapper, serializer, typeMapper.GetId(tessage.GetType()), tessage.GetType(), tessageId, body, tessage is IExactlyOnceTessage);
      }

      OutGoing(ITypeMapper typeMapper, IRemotableTessageSerializer serializer, TypeId typeId, Type type, Guid id, string body, bool isExactlyOnceDeliveryTessage)
      {
         TypeMapper = typeMapper;
         Serializer = serializer;
         IsExactlyOnceDeliveryTessage = isExactlyOnceDeliveryTessage;
         Type = typeId;
         Id = id;
         Body = body;
         TessageTypeEnum = GetTessageTypeEnum(type);
      }

      internal InComing ToIncoming() =>
         new(Body, Type, Id, TypeMapper, Serializer);
   }

   static TransportTessageType GetTessageTypeEnum(Type tessageType)
   {
      if(typeof(IRemotableTuery<object>).IsAssignableFrom(tessageType))
         return TransportTessageType.NonTransactionalTuery;
      if(typeof(IAtMostOnceTommand<object>).IsAssignableFrom(tessageType))
         return TransportTessageType.AtMostOnceTommandWithReturnValue;
      if(typeof(IAtMostOnceHypermediaTommand).IsAssignableFrom(tessageType))
         return TransportTessageType.AtMostOnceTommand;
      else if(typeof(IExactlyOnceTevent).IsAssignableFrom(tessageType))
         return TransportTessageType.ExactlyOnceTevent;
      if(typeof(IExactlyOnceTommand).IsAssignableFrom(tessageType))
         return TransportTessageType.ExactlyOnceTommand;
      else
         throw new ArgumentOutOfRangeException();
   }
}
