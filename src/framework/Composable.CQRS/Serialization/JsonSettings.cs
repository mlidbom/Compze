using Newtonsoft.Json;

namespace Composable.Serialization;

static class JsonSettings
{
    internal static readonly JsonSerializerSettings JsonSerializerSettings =
        new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = IncludeMembersWithPrivateSettersResolver.Instance
        };

    public static readonly JsonSerializerSettings SqlEventStoreSerializerSettings =
        new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql.Instance
        };

}