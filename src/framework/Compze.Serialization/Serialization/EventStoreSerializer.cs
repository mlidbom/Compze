using System;
using System.Text.RegularExpressions;
using Compze.Messaging;
using Compze.Persistence.EventStore;
using Compze.Refactoring.Naming;
using Compze.SystemCE;
using Newtonsoft.Json;

namespace Compze.Serialization;

class RenamingSupportingJsonSerializer : IJsonSerializer
{
   readonly JsonSerializerSettings _jsonSettings;
   readonly RenamingDecorator _renamingDecorator;
   protected internal RenamingSupportingJsonSerializer(JsonSerializerSettings jsonSettings, ITypeMapper typeMapper)
   {
      _jsonSettings = jsonSettings;
      _renamingDecorator = new RenamingDecorator(typeMapper);
   }

   public string Serialize(object instance)
   {
      var json = JsonConvert.SerializeObject(instance, Formatting.Indented, _jsonSettings);
      json = _renamingDecorator.ReplaceTypeNames(json);
      return json;
   }

   public object Deserialize(Type type, string json)
   {
      json = _renamingDecorator.RestoreTypeNames(json);
      return JsonConvert.DeserializeObject(json, type, _jsonSettings)!;
   }
}

class RenamingDecorator(ITypeMapper typeMapper)
{
   readonly ITypeMapper _typeMapper = typeMapper;

   static readonly OptimizedLazy<Regex> FindTypeNames = new(() => new Regex(@"""\$type""\: ""([^""]*)""", RegexOptions.Compiled));

   public string ReplaceTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeNamesWithTypeIds);

   string ReplaceTypeNamesWithTypeIds(Match match)
   {
      var type = Type.GetType(match.Groups[1].Value);
      var typeId = _typeMapper.GetId(type!);
      return $@"""$type"": ""{typeId.GuidValue}""";
   }

   public string RestoreTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeIdsWithTypeNames);

   string ReplaceTypeIdsWithTypeNames(Match match)
   {
      var typeId = new TypeId(Guid.Parse(match.Groups[1].Value));
      var type = _typeMapper.GetType(typeId);
      return $@"""$type"": ""{type.AssemblyQualifiedName}""";
   }
}


class EventStoreSerializer(ITypeMapper typeMapper) : IEventStoreSerializer
{
   internal static readonly JsonSerializerSettings JsonSettings = Serialization.JsonSettings.SqlEventStoreSerializerSettings;

   readonly RenamingSupportingJsonSerializer _serializer = new(JsonSettings, typeMapper);

   public string Serialize(AggregateEvent @event) => _serializer.Serialize(@event);
   public IAggregateEvent Deserialize(Type eventType, string json) => (IAggregateEvent)_serializer.Deserialize(eventType, json);
}

class DocumentDbSerializer(ITypeMapper typeMapper) : RenamingSupportingJsonSerializer(JsonSettings.SqlEventStoreSerializerSettings, typeMapper), IDocumentDbSerializer;

class RemotableMessageSerializer(ITypeMapper typeMapper) : IRemotableMessageSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer = new(Serialization.JsonSettings.JsonSerializerSettings, typeMapper);

   public string SerializeResponse(object response) => _serializer.Serialize(response);
   public object DeserializeResponse(Type responseType, string json) => _serializer.Deserialize(responseType, json);

   public string SerializeMessage(IRemotableMessage message) => _serializer.Serialize(message);
   public IRemotableMessage DeserializeMessage(Type messageType, string json) => (IRemotableMessage)_serializer.Deserialize(messageType, json);
}