using Compze.Core.Serialization.Internal.DbPool;
//using Compze.Serialization.Newtonsoft.Private.PrimitiveWrappers;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Compze.Serialization.Newtonsoft.Private.DbPool;

static class NewtonsoftSharedObjectSerializerRegistrar
{
   internal static IComponentRegistrar NewtonsoftSharedObjectSerializer(this IComponentRegistrar registrar) =>
      DbPool.NewtonsoftSharedObjectSerializer.RegisterWith(registrar);
}

class NewtonsoftSharedObjectSerializer : ISharedObjectSerializer
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ISharedObjectSerializer>()
                                     .CreatedBy(() => new NewtonsoftSharedObjectSerializer()));

   static readonly JsonSerializerSettings JsonSettings = JsonSettings = new JsonSerializerSettings
                                                                        {
                                                                           ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                                                           //Converters = new List<JsonConverter> { new ValueWrapperConverter() },
                                                                           ContractResolver = IncludeMembersWithPrivateSettersResolver.Instance
                                                                        };

   public string Serialize(object instance) => JsonConvert.SerializeObject(instance, Formatting.Indented, JsonSettings);

   public TShared Deserialize<TShared>(string serialized)
      where TShared : class =>
      JsonConvert.DeserializeObject<TShared>(serialized, JsonSettings).NotNull();
}
