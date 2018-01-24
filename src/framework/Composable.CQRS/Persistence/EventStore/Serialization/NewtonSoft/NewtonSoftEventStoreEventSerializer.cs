using System;
using Newtonsoft.Json;

namespace Composable.Persistence.EventStore.Serialization.NewtonSoft
{
    class NewtonSoftEventStoreEventSerializer : IEventStoreEventSerializer
    {
        internal static readonly JsonSerializerSettings JsonSettings = Composable.NewtonSoft.JsonSettings.SqlEventStoreSerializerSettings;

        public string Serialize(object @event) => JsonConvert.SerializeObject(@event, Formatting.Indented, JsonSettings);

        public IAggregateEvent Deserialize(Type eventType, string eventData) => (IAggregateEvent)JsonConvert.DeserializeObject(eventData, eventType, JsonSettings);
    }
}