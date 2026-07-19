using Compze.TypeIdentifiers;
using Compze.Tessaging.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Abstractions.Public;
using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Internals.Serialization.Newtonsoft.Private.Typermedia;

class NewtonsoftTypermediaSerializer : ITypermediaSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaSerializer>()
                                     .CreatedBy((ITypeMap typeMap) => new NewtonsoftTypermediaSerializer(typeMap)));

   NewtonsoftTypermediaSerializer(ITypeMap typeMap) => _serializer = new RenamingSupportingJsonSerializer(RenamingAndNonPublicMembersSupportingJsonSettings.Typermedia, typeMap);

   public string SerializeTessage(ITypermediaTessage tessage) => _serializer.Serialize(tessage);
   public ITypermediaTessage DeserializeTessage(Type tessageType, string json) => (ITypermediaTessage)_serializer.Deserialize(tessageType, json);

   public string SerializeResult(object result) => _serializer.Serialize(result);
   public TResult DeserializeResult<TResult>(string json) => (TResult)_serializer.Deserialize(typeof(TResult), json);
}
