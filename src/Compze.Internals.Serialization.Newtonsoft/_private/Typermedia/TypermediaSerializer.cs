using Compze.Tessaging.Typermedia._internal;
using Compze.Abstractions.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Internals.Serialization.Newtonsoft._private.Typermedia;

class NewtonsoftTypermediaSerializer : ITypermediaSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar
        .RequireMappedTypesFromAssemblyContaining<TentityId>()
        .RequireMappedTypesFromAssemblyContaining<IExactlyOnceTevent>()
        .RequireMappedTypesFromAssemblyContaining<ITaggregateTevent>()
        .Register(Singleton.For<ITypermediaSerializer>()
                           .CreatedBy((ITypeMap typeMap) => new NewtonsoftTypermediaSerializer(typeMap)));

   NewtonsoftTypermediaSerializer(ITypeMap typeMap) => _serializer = new RenamingSupportingJsonSerializer(RenamingAndNonPublicMembersSupportingJsonSettings.Typermedia, typeMap);

   public string SerializeTessage(ITypermediaTessage tessage) => _serializer.Serialize(tessage);
   public ITypermediaTessage DeserializeTessage(Type tessageType, string json) => (ITypermediaTessage)_serializer.Deserialize(tessageType, json);

   public string SerializeResult(object result) => _serializer.Serialize(result);
   public TResult DeserializeResult<TResult>(string json) => (TResult)_serializer.Deserialize(typeof(TResult), json);
}
