using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Serialization.Newtonsoft.Private.Tessaging;

static class RemotableTessageSerializerRegistrar
{
   public static IComponentRegistrar NewtonSoftRemotableTessageSerializer(this IComponentRegistrar registrar)
      => registrar.Register(NewtonsoftRemotableTessageSerializer.RegisterWith);
}

class NewtonsoftRemotableTessageSerializer : IRemotableTessageSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRemotableTessageSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new NewtonsoftRemotableTessageSerializer(typeMapper)));

   NewtonsoftRemotableTessageSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(RenamingAndNonPublicMembersSupportingJsonSettings.Tessaging, typeMapper);

   public string SerializeResponse(object response) => _serializer.Serialize(response);
   public TResponse DeserializeResponse<TResponse>(string json) => (TResponse)_serializer.Deserialize(typeof(TResponse), json);

   public string SerializeTessage(IMessage tessage) => _serializer.Serialize(tessage);
   public IMessage DeserializeTessage(Type tessageType, string json) => (IMessage)_serializer.Deserialize(tessageType, json);
}
