using System;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Newtonsoft.Json;

namespace Compze.Serialization.Newtonsoft.Private.TeventStore;

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
                  .CreatedBy((ITypeMapper typeMapper) => new NewtonsoftTeventStoreSerializer(typeMapper)));

   NewtonsoftTeventStoreSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings, typeMapper);

   public string Serialize(TaggregateTevent tevent) => _serializer.Serialize(tevent);
   public ITaggregateTevent Deserialize(Type teventType, string json) => (ITaggregateTevent)_serializer.Deserialize(teventType, json);
}

