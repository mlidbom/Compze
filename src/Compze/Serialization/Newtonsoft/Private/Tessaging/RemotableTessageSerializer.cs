using System;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.Private.Tessaging;

static class RemotableTessageSerializerRegistrar
{
   internal static IComponentRegistrar NewtonSoftRemotableTessageSerializer(this IComponentRegistrar registrar)
      => registrar.Register(Tessaging.NewtonsoftRemotableTessageSerializer.RegisterWith);
}

class NewtonsoftRemotableTessageSerializer : IRemotableTessageSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRemotableTessageSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new NewtonsoftRemotableTessageSerializer(typeMapper)));

   NewtonsoftRemotableTessageSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(RenamingAndNonPublicMembersSupportingJsonSettings.Tessaging, typeMapper);

   public string SerializeResponse(object response) => _serializer.Serialize(response);
   public TResponse DeserializeResponse<TResponse>(string json) => (TResponse)_serializer.Deserialize(typeof(TResponse), json);

   public string SerializeTessage(IRemotableTessage tessage) => _serializer.Serialize(tessage);
   public IRemotableTessage DeserializeTessage(Type tessageType, string json) => (IRemotableTessage)_serializer.Deserialize(tessageType, json);
}
