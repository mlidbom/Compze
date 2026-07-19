using Compze.TypeIdentifiers;
using Compze.Tessaging.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Abstractions.Public;
using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Internals.Serialization.Newtonsoft.Private.Tessaging;

class NewtonsoftTessagingSerializer : ITessagingSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessagingSerializer>()
                                     .CreatedBy((ITypeMap typeMap) => new NewtonsoftTessagingSerializer(typeMap)));

   NewtonsoftTessagingSerializer(ITypeMap typeMap) => _serializer = new RenamingSupportingJsonSerializer(RenamingAndNonPublicMembersSupportingJsonSettings.Tessaging, typeMap);

   public string SerializeTessage(ITessage tessage) => _serializer.Serialize(tessage);
   public ITessage DeserializeTessage(Type tessageType, string json) => (ITessage)_serializer.Deserialize(tessageType, json);
}
