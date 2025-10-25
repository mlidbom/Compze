using System;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.Tessaging;

static class RemotableTessageSerializerRegistrar
{
   internal static IComponentRegistrar RemotableTessageSerializer(this IComponentRegistrar registrar)
      => registrar.Register(Tessaging.RemotableTessageSerializer.RegisterWith);
}

class RemotableTessageSerializer : IRemotableTessageSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRemotableTessageSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new RemotableTessageSerializer(typeMapper)));

   RemotableTessageSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(RenamingAndNonPublicMembersSupportingJSONSettings.DocumentDb, typeMapper);

   public string SerializeResponse(object response) => _serializer.Serialize(response);
   public object DeserializeResponse(Type responseType, string json) => _serializer.Deserialize(responseType, json);

   public string SerializeTessage(IRemotableTessage tessage) => _serializer.Serialize(tessage);
   public IRemotableTessage DeserializeTessage(Type tessageType, string json) => (IRemotableTessage)_serializer.Deserialize(tessageType, json);
}
