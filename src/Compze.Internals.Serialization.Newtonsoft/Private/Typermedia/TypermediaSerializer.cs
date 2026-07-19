using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;

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
