using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Contracts;
using System;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

//todo: split out TyperMediaTransportTessage
public static class TransportTessage
{
   public class InComing
   {
      internal readonly TessageId TessageId;
      readonly IRemotableTessageSerializer _serializer;
      internal readonly string Body;
      internal readonly TypeId TessageTypeId;
      readonly Type _tessageType;
      internal readonly TransportTessageType TessageTypeEnum;

      ITessage? _tessage;

      internal ITessage DeserializeTessageAndCacheForNextCall()
      {
         if(_tessage == null)
         {
            _tessage = _serializer.DeserializeTessage(_tessageType, Body);

            Contract.State.Assert(_tessage is not IExactlyOnceTessage actualTessage || TessageId == actualTessage.Id);
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
      internal bool IsExactlyOnceDeliveryTessage { get; }
      internal IRemotableTessage Tessage { get; }
      internal readonly TessageId TessageId;

      internal readonly TypeId Type;
      internal readonly string Body;
      internal readonly TransportTessageType TessageTypeEnum;

      internal static OutGoing Create(IRemotableTessage tessage, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         var tessageId = (tessage as IAtMostOnceTessage)?.Id ?? new TessageId();
         var body = serializer.SerializeTessage(tessage);
         return new OutGoing(typeMapper.GetId(tessage.GetType()), tessage.GetType(), tessageId, body, tessage is IExactlyOnceTessage, tessage);
      }

      OutGoing(TypeId typeId, Type type, TessageId id, string body, bool isExactlyOnceDeliveryTessage, IRemotableTessage tessage)
      {
         IsExactlyOnceDeliveryTessage = isExactlyOnceDeliveryTessage;
         Tessage = tessage;
         Type = typeId;
         TessageId = (tessage as IAtMostOnceTessage)?.Id ?? new TessageId();
         Body = body;
         TessageTypeEnum = type.TransportTessageType();
      }
   }
}
