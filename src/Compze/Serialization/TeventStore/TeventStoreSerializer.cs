using System;
using System.Text.RegularExpressions;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;

namespace Compze.Serialization.Newtonsoft.TeventStore;

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
   internal static readonly JsonSerializerSettings JsonSettings = Newtonsoft.RenamingAndNonPublicMembersSupportingJSONSettings.TeventStore;

   readonly RenamingSupportingJsonSerializer _serializer;

   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<ITeventStoreSerializer>()
                  .CreatedBy((ITypeMapper typeMapper) => new TeventStoreSerializer(typeMapper)));

   internal TeventStoreSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings, typeMapper);

   public string Serialize(TaggregateTevent tevent) => _serializer.Serialize(tevent);
   public ITaggregateTevent Deserialize(Type teventType, string json) => (ITaggregateTevent)_serializer.Deserialize(teventType, json);
}

