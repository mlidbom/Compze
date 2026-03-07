using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
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

      public ITessage DeserializeTessageAndCacheForNextCall()
      {
         if(_tessage == null)
         {
            _tessage = (ITessage)_serializer.DeserializeTessage(_tessageType, Body);

            State.Assert(_tessage is not IExactlyOnceTessage actualTessage || TessageId == actualTessage.Id);
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
      internal IRemotableTessage Tessage { get; }
      internal readonly TessageId TessageId;

      internal readonly TypeId Type;
      internal readonly string Body;
      internal readonly TransportTessageType TessageTypeEnum;

      internal static OutGoing Create(IRemotableTessage tessage, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         var body = serializer.SerializeTessage(tessage);
         return new OutGoing(typeMapper.GetId(tessage.GetType()), tessage.GetType(), body, tessage);
      }

      OutGoing(TypeId typeId, Type type, string body, IRemotableTessage tessage)
      {
         Tessage = tessage;
         Type = typeId;
         TessageId = (tessage as IAtMostOnceTessage)?.Id ?? new TessageId();
         Body = body;
         TessageTypeEnum = type.TransportTessageType();
      }
   }
}
