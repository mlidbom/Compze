using System;
using System.Text.RegularExpressions;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;

namespace Compze.Serialization.Newtonsoft;

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

class TeventStoreSerializer : ITeventStoreSerializer
{
   internal static readonly JsonSerializerSettings JsonSettings = Newtonsoft.JsonSettings.SqlTeventStoreSerializerSettings;

   readonly RenamingSupportingJsonSerializer _serializer;

   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<ITeventStoreSerializer>()
                  .CreatedBy((ITypeMapper typeMapper) => new TeventStoreSerializer(typeMapper)));

   internal TeventStoreSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings, typeMapper);

   public string Serialize(TaggregateTevent tevent) => _serializer.Serialize(tevent);
   public ITaggregateTevent Deserialize(Type teventType, string json) => (ITaggregateTevent)_serializer.Deserialize(teventType, json);
}

class DocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
{
   DocumentDbSerializer(ITypeMapper typeMapper) : base(JsonSettings.SqlTeventStoreSerializerSettings, typeMapper) {}

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IDocumentDbSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new DocumentDbSerializer(typeMapper)));
}

static class RemotableTessageSerializerRegistrar
{
   internal static IComponentRegistrar RemotableTessageSerializer(this IComponentRegistrar registrar)
      => registrar.Register(Newtonsoft.RemotableTessageSerializer.RegisterWith);
}

class RemotableTessageSerializer : IRemotableTessageSerializer
{
   readonly RenamingSupportingJsonSerializer _serializer;

   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRemotableTessageSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new RemotableTessageSerializer(typeMapper)));

   RemotableTessageSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings.JsonSerializerSettings, typeMapper);

   public string SerializeResponse(object response) => _serializer.Serialize(response);
   public object DeserializeResponse(Type responseType, string json) => _serializer.Deserialize(responseType, json);

   public string SerializeTessage(IRemotableTessage tessage) => _serializer.Serialize(tessage);
   public IRemotableTessage DeserializeTessage(Type tessageType, string json) => (IRemotableTessage)_serializer.Deserialize(tessageType, json);
}
