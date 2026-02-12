using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.Contracts;
using System;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

//todo: split out TyperMediaTransportTessage
public static class TransportTessage
{
   public class InComing
   {
      public readonly TessageId TessageId;
      readonly IRemotableTessageSerializer _serializer;
      public readonly string Body;
      public readonly TypeId TessageTypeId;
      readonly Type _tessageType;
      public readonly TransportTessageType TessageTypeEnum;

      ITessage? _tessage;

      public ITessage DeserializeTessageAndCacheForNextCall()
      {
         if(_tessage == null)
         {
            _tessage = _serializer.DeserializeTessage(_tessageType, Body);

            Assert.State.Is(_tessage is not IExactlyOnceTessage actualTessage || TessageId == actualTessage.Id);
         }

         return _tessage;
      }

      public InComing(string body, TypeId tessageTypeId, TessageId tessageId, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         _serializer = serializer;
         Body = body;
         TessageTypeId = tessageTypeId;
         _tessageType = typeMapper.GetType(tessageTypeId);
         TessageTypeEnum = _tessageType.TransportTessageType();
         TessageId = tessageId;
      }
   }

   public class OutGoing
   {
      public bool IsExactlyOnceDeliveryTessage { get; }
      public readonly TessageId TessageId;

      public readonly TypeId Type;
      public readonly string Body;
      public readonly TransportTessageType TessageTypeEnum;

      public static OutGoing Create(IRemotableTessage tessage, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         var tessageId = (tessage as IAtMostOnceTessage)?.Id ?? new TessageId();
         var body = serializer.SerializeTessage(tessage);
         return new OutGoing(typeMapper.GetId(tessage.GetType()), tessage.GetType(), tessageId, body, tessage is IExactlyOnceTessage, tessage, typeMapper, serializer);
      }

      OutGoing(TypeId typeId, Type type, TessageId id, string body, bool isExactlyOnceDeliveryTessage, IRemotableTessage tessage, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         IsExactlyOnceDeliveryTessage = isExactlyOnceDeliveryTessage;
         Type = typeId;
         TessageId = (tessage as IAtMostOnceTessage)?.Id ?? new TessageId();
         Body = body;
         TessageTypeEnum = type.TransportTessageType();
      }
   }
}