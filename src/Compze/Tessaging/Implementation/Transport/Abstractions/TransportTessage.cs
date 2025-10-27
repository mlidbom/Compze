using System;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.Contracts;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

//todo: split out TyperMediaTransportTessage
static class TransportTessage
{
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
         TessageTypeEnum = _tessageType.TransportTessageType();
         TessageId = tessageId;
      }
   }

   internal class OutGoing
   {
      public bool IsExactlyOnceDeliveryTessage { get; }
      public readonly Guid Id;

      internal readonly TypeId Type;
      internal readonly string Body;
      internal readonly TransportTessageType TessageTypeEnum;

      public static OutGoing Create(IRemotableTessage tessage, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         var tessageId = (tessage as IAtMostOnceTessage)?.TessageId ?? Guid.CreateVersion7();
         var body = serializer.SerializeTessage(tessage);
         return new OutGoing(typeMapper.GetId(tessage.GetType()), tessage.GetType(), tessageId, body, tessage is IExactlyOnceTessage);
      }

      OutGoing(TypeId typeId, Type type, Guid id, string body, bool isExactlyOnceDeliveryTessage)
      {
         IsExactlyOnceDeliveryTessage = isExactlyOnceDeliveryTessage;
         Type = typeId;
         Id = id;
         Body = body;
         TessageTypeEnum = type.TransportTessageType();
      }
   }
}