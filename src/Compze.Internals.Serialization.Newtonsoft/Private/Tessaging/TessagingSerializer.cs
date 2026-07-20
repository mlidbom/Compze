using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.TessageBus;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Internals.Serialization.Newtonsoft.Private.Tessaging;

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
