using Compze.Abstractions;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageBus._internal;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Serialization.Newtonsoft._private.Tessaging;

class NewtonsoftTessagingSerializer : ITessagingSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar
        .RequireMappedTypesFromAssemblyContaining<TentityId>()
        .RequireMappedTypesFromAssemblyContaining<IExactlyOnceTevent>()
        .RequireMappedTypesFromAssemblyContaining<ITessagingSerializer>()
        .Register(Singleton.For<ITessagingSerializer>()
                                     .CreatedBy((ITypeMap typeMap) => new NewtonsoftTessagingSerializer(typeMap)));

   NewtonsoftTessagingSerializer(ITypeMap typeMap) => _serializer = new RenamingSupportingJsonSerializer(RenamingAndNonPublicMembersSupportingJsonSettings.Tessaging, typeMap);

   public string SerializeTessage(ITessage tessage) => _serializer.Serialize(tessage);
   public ITessage DeserializeTessage(Type tessageType, string json) => (ITessage)_serializer.Deserialize(tessageType, json);
}
