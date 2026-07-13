using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

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
            _tessage = (ITessage)_serializer.DeserializeTessage(_tessageType, Body);

            State.Assert(_tessage switch
                         {
                            IPublisherIdentifyingTevent<IExactlyOnceTevent> wrapper => TessageId == wrapper.Tevent.Id,
                            IAtMostOnceTessage atMostOnce => TessageId == atMostOnce.Id,
                            _ => true
                         });
         }

         return _tessage;
      }

      public InComing(string body, string persistedTypeString, TessageId tessageId, ITypeMap typeMap, IRemotableTessageSerializer serializer)
      {
         _serializer = serializer;
         Body = body;
         TessageTypeId = typeMap.GetId(persistedTypeString);
         _tessageType = TessageTypeId.Type;
         TessageTypeEnum = _tessageType.TransportTessageType();
         TessageId = tessageId;
      }
   }

   public class OutGoing
   {
      internal ITessage Tessage { get; }
      internal readonly TessageId TessageId;

      internal readonly TypeId Type;
      internal readonly string Body;
      internal readonly TransportTessageType TessageTypeEnum;

      internal static OutGoing Create(ITessage tessage, TessageId dedupId, ITypeMap typeMap, IRemotableTessageSerializer serializer)
      {
         var body = serializer.SerializeTessage(tessage);
         return new OutGoing(typeMap.GetId(tessage.GetType()), tessage.GetType(), body, tessage, dedupId);
      }

      OutGoing(TypeId typeId, Type type, string body, ITessage tessage, TessageId tessageId)
      {
         Tessage = tessage;
         Type = typeId;
         TessageId = tessageId;
         Body = body;
         TessageTypeEnum = type.TransportTessageType();
      }
   }
}
