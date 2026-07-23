using Compze.Contracts;
using Compze.Tessaging.TessageBus._internal;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging._internal.Transport;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging._private.Transport;

static class TransportTessage
{
   public class InComing
   {
      internal readonly TessageId TessageId;
      readonly ITessagingSerializer _serializer;
      internal readonly string Body;
      internal readonly TypeId TessageTypeId;
      readonly Type _tessageType;
      internal readonly TransportTessageType TessageTypeEnum;

      ///<summary>The tessage's coordinates in its pair's delivery stream — what inbox admission is decided on. Carried by every<br/>
      /// exactly-once tessage arriving over the wire; null on the tiers that have no delivery stream, and on a tessage the<br/>
      /// inbox recovery scan reloads from its own rows, which was admitted before the crash and faces no admission check again.</summary>
      internal readonly DeliveryStreamPosition? DeliveryStreamPosition;

      ITessage? _tessage;

      internal ITessage DeserializeTessageAndCacheForNextCall()
      {
         if(_tessage == null)
         {
            _tessage = _serializer.DeserializeTessage(_tessageType, Body);

            State.Assert(_tessage switch
                         {
                            IPublisherTevent<IExactlyOnceTevent> wrapper => TessageId == wrapper.Tevent.Id,
                            IAtMostOnceTessage atMostOnce => TessageId == atMostOnce.Id,
                            _ => true
                         });
         }

         return _tessage;
      }

      internal InComing(string body, string persistedTypeString, TessageId tessageId, ITypeMap typeMap, ITessagingSerializer serializer, DeliveryStreamPosition? deliveryStreamPosition = null)
      {
         _serializer = serializer;
         Body = body;
         TessageTypeId = typeMap.GetId(persistedTypeString);
         _tessageType = TessageTypeId.Type;
         TessageTypeEnum = _tessageType.TransportTessageType();
         TessageId = tessageId;
         DeliveryStreamPosition = deliveryStreamPosition;
      }
   }

   public class OutGoing
   {
      internal ITessage Tessage { get; }
      internal readonly TessageId TessageId;

      internal readonly TypeId Type;
      internal readonly string Body;
      internal readonly TransportTessageType TessageTypeEnum;

      ///<summary>The tessage's sequence number in the sender-receiver pair's delivery stream, assigned by the outbox save<br/>
      /// (see <see cref="DeliveryStreamPosition"/>): the exactly-once send queue's ordering key, and what the wire envelope<br/>
      /// carries to the receiver's inbox. Null on the best-effort tier, which has no delivery stream sequence.</summary>
      internal readonly long? DeliveryStreamSequenceNumber;

      internal static OutGoing Create(ITessage tessage, TessageId dedupId, ITypeMap typeMap, ITessagingSerializer serializer, long? deliveryStreamSequenceNumber = null)
      {
         var body = serializer.SerializeTessage(tessage);
         return new OutGoing(typeMap.GetId(tessage.GetType()), tessage.GetType(), body, tessage, dedupId, deliveryStreamSequenceNumber);
      }

      OutGoing(TypeId typeId, Type type, string body, ITessage tessage, TessageId tessageId, long? deliveryStreamSequenceNumber)
      {
         Tessage = tessage;
         Type = typeId;
         TessageId = tessageId;
         Body = body;
         TessageTypeEnum = type.TransportTessageType();
         DeliveryStreamSequenceNumber = deliveryStreamSequenceNumber;
      }
   }
}
