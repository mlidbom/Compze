using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#pragma warning disable CA2326 // TypeNameHandling is safe for testing serialization
#pragma warning disable CA2327

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

static class TestingJsonSettings
{
   internal static readonly JsonSerializerSettings All = CreateSettings(new AllMembersContractResolver());
   internal static readonly JsonSerializerSettings Internal = CreateSettings(new InternalMembersContractResolver());
   internal static readonly JsonSerializerSettings Public = CreateSettings(new PublicMembersContractResolver());

   static JsonSerializerSettings CreateSettings(IContractResolver resolver) =>
      new()
      {
         TypeNameHandling = TypeNameHandling.All,
         TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
         Formatting = Formatting.Indented,
         ContractResolver = resolver,
         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
         MaxDepth = 32
      };
}

abstract class MemberFilteringContractResolver : DefaultContractResolver
{
   protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
   {
      var property = base.CreateProperty(member, memberSerialization);
      property.Readable = true;
      property.Writable = true;
      return property;
   }

   protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
   {
      var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                              .Where(ShouldIncludeProperty);

      var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                          .Where(ShouldIncludeField);

      return allProperties.Cast<MemberInfo>()
                          .Concat(allFields)
                          .Select(m => CreateProperty(m, memberSerialization))
                          .ToList();
   }

   protected abstract bool ShouldIncludeProperty(PropertyInfo property);
   protected abstract bool ShouldIncludeField(FieldInfo field);
}

class AllMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldIncludeProperty(PropertyInfo property) => true;
   protected override bool ShouldIncludeField(FieldInfo field) => true;
}

class InternalMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldIncludeProperty(PropertyInfo property) =>
      property.GetMethod is { IsAssembly: true } or { IsFamilyOrAssembly: true } or { IsFamily: true };

   protected override bool ShouldIncludeField(FieldInfo field) =>
      field.IsAssembly || field.IsFamilyOrAssembly || field.IsFamily;
}

class PublicMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldIncludeProperty(PropertyInfo property) => property.GetMethod?.IsPublic ?? false;
   protected override bool ShouldIncludeField(FieldInfo field) => field.IsPublic;
}
