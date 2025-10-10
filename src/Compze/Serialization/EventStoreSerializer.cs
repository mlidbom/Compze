using System;
using System.Text.RegularExpressions;
using Compze.Abstractions.Internal;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
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

   static readonly LazyCE<Regex> FindTypeNames = new(() => new Regex("""
                                                                            "\$type"\: "([^"]*)"
                                                                            """,
                                                                            RegexOptions.Compiled));

   public string ReplaceTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeNamesWithTypeIds);

   string ReplaceTypeNamesWithTypeIds(Match match)
   {
      var type = Type.GetType(match.Groups[1].Value);
      var typeId = _typeMapper.GetId(type!);
      return $"""
              "$type": "{typeId.GuidValue}"
              """;
   }

   public string RestoreTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeIdsWithTypeNames);

   string ReplaceTypeIdsWithTypeNames(Match match)
   {
      var typeId = new TypeId(Guid.Parse(match.Groups[1].Value));
      var type = _typeMapper.GetType(typeId);
      return $"""
              "$type": "{type.AssemblyQualifiedName}"
              """;
   }
}

class EventStoreSerializer : IEventStoreSerializer
{
   internal static readonly JsonSerializerSettings JsonSettings = Serialization.JsonSettings.SqlEventStoreSerializerSettings;

   readonly RenamingSupportingJsonSerializer _serializer;

   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(
         Singleton.For<IEventStoreSerializer>()
                  .CreatedBy((ITypeMapper typeMapper) => new EventStoreSerializer(typeMapper)));

   internal EventStoreSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings, typeMapper);

   public string Serialize(AggregateEvent @event) => _serializer.Serialize(@event);
   public IAggregateEvent Deserialize(Type eventType, string json) => (IAggregateEvent)_serializer.Deserialize(eventType, json);
}

class DocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
{
   DocumentDbSerializer(ITypeMapper typeMapper) : base(JsonSettings.SqlEventStoreSerializerSettings, typeMapper) {}

   public static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(Singleton.For<IDocumentDbSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new DocumentDbSerializer(typeMapper)));
}

static class RemotableMessageSerializerRegistrar
{
   internal static IDependencyRegistrar RemotableMessageSerializer(this IDependencyRegistrar registrar)
      => registrar.Register(Serialization.RemotableMessageSerializer.RegisterWith);
}

class RemotableMessageSerializer : IRemotableMessageSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(Singleton.For<IRemotableMessageSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new RemotableMessageSerializer(typeMapper)));

   RemotableMessageSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(Serialization.JsonSettings.JsonSerializerSettings, typeMapper);

   public string SerializeResponse(object response) => _serializer.Serialize(response);
   public object DeserializeResponse(Type responseType, string json) => _serializer.Deserialize(responseType, json);

   public string SerializeMessage(IRemotableMessage message) => _serializer.Serialize(message);
   public IRemotableMessage DeserializeMessage(Type messageType, string json) => (IRemotableMessage)_serializer.Deserialize(messageType, json);
}
