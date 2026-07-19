using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Internal;
using Newtonsoft.Json;

namespace Compze.Internals.Serialization.Newtonsoft.Private.TeventStore;

public class NewtonsoftTeventStoreSerializer : ITeventStoreSerializer
{
   public static readonly JsonSerializerSettings JsonSettings = RenamingAndNonPublicMembersSupportingJsonSettings.TeventStore;

   readonly RenamingSupportingJsonSerializer _serializer;

   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<ITeventStoreSerializer>()
                  .CreatedBy((ITypeMap typeMap) => new NewtonsoftTeventStoreSerializer(typeMap)));

   NewtonsoftTeventStoreSerializer(ITypeMap typeMap) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings, typeMap);

   public string Serialize(ITaggregateTevent<ITaggregateTevent> wrappedTevent) => _serializer.Serialize(wrappedTevent);
   public ITaggregateTevent<ITaggregateTevent> Deserialize(Type wrapperTeventType, string json) => (ITaggregateTevent<ITaggregateTevent>)_serializer.Deserialize(wrapperTeventType, json);
}

