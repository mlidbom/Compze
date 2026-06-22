using Compze.TypeIdentifiers;
using Compze.Tessaging.Teventive.TeventStore.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Public.Taggregates.Tevents.Public;
using Newtonsoft.Json;

namespace Compze.Internals.Serialization.Newtonsoft.Private.TeventStore;

static class NewtonsoftTeventStoreSerializerRegistrar
{
   public static IComponentRegistrar NewtonsoftTeventStoreSerializer(this IComponentRegistrar registrar) =>
      TeventStore.NewtonsoftTeventStoreSerializer.RegisterWith(registrar);
}

public class NewtonsoftTeventStoreSerializer : ITeventStoreSerializer
{
   public static readonly JsonSerializerSettings JsonSettings = RenamingAndNonPublicMembersSupportingJsonSettings.TeventStore;

   readonly RenamingSupportingJsonSerializer _serializer;

   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<ITeventStoreSerializer>()
                  .CreatedBy((ITypeMap typeMap) => new NewtonsoftTeventStoreSerializer(typeMap)));

   NewtonsoftTeventStoreSerializer(ITypeMap typeMap) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings, typeMap);

   public string Serialize(TaggregateTevent tevent) => _serializer.Serialize(tevent);
   public ITaggregateTevent Deserialize(Type teventType, string json) => (ITaggregateTevent)_serializer.Deserialize(teventType, json);
}

