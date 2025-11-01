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
   internal static readonly JsonSerializerSettings All =
      new()
      {
         TypeNameHandling = TypeNameHandling.All,
         TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
         Formatting = Formatting.Indented,
         ContractResolver = new AllMembersContractResolver(),
         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
         MaxDepth = 32
      };

   internal static readonly JsonSerializerSettings Internal =
      new()
      {
         TypeNameHandling = TypeNameHandling.All,
         TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
         Formatting = Formatting.Indented,
         ContractResolver = new InternalMembersContractResolver(),
         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
         MaxDepth = 32
      };

   internal static readonly JsonSerializerSettings Public =
      new()
      {
         TypeNameHandling = TypeNameHandling.All,
         TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
         Formatting = Formatting.Indented,
         ContractResolver = new PublicMembersContractResolver(),
         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
         MaxDepth = 32
      };
}

class AllMembersContractResolver : DefaultContractResolver
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
      var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           .Select(p => CreateProperty(p, memberSerialization))
                           .ToList();

      var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                       .Select(f => CreateProperty(f, memberSerialization))
                       .ToList();

      return properties.Union(fields).ToList();
   }
}

class InternalMembersContractResolver : DefaultContractResolver
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
      var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           .Where(p => p.GetMethod is { IsAssembly: true } or { IsFamilyOrAssembly: true } or { IsFamily: true })
                           .Select(p => CreateProperty(p, memberSerialization))
                           .ToList();

      var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                       .Where(f => f.IsAssembly || f.IsFamilyOrAssembly || f.IsFamily)
                       .Select(f => CreateProperty(f, memberSerialization))
                       .ToList();

      return properties.Union(fields).ToList();
   }
}

class PublicMembersContractResolver : DefaultContractResolver
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
      var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .Select(p => CreateProperty(p, memberSerialization))
                           .ToList();

      var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                       .Select(f => CreateProperty(f, memberSerialization))
                       .ToList();

      return properties.Union(fields).ToList();
   }
}
