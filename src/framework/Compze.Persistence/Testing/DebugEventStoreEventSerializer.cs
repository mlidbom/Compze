using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable CA2326 //Todo about this resides elsewhere search for CA2326 to find it
#pragma warning disable CA2327

namespace Compze.Testing;

static class DebugEventStoreEventSerializer
{
   class DebugPrintingContractsResolver : DefaultContractResolver
   {
      protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
      {
         var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                         .Select(p => CreateProperty(p, memberSerialization))
                         .ToList();
         props.ForEach(p => { p.Writable = true; p.Readable = true; });
         return props;
      }
   }

   static readonly JsonSerializerSettings JsonSettings =
      new()
      {
         TypeNameHandling = TypeNameHandling.Auto,
         ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
         ContractResolver = new DebugPrintingContractsResolver(),
         Error = (_, err) => err.ErrorContext.Handled = true
      };

   public static string Serialize(object @event, Formatting formatting) => JsonConvert.SerializeObject(@event, formatting, JsonSettings);
}