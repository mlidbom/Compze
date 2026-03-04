using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

#pragma warning disable CA2326 // TypeNameHandling is safe for testing serialization
#pragma warning disable CA2327

namespace Compze.Must.Serialization;

static class TestingJsonSettings
{
   public static readonly JsonSerializerSettings AllMembers = CreateSettings(new AllMembersContractResolver());
   public static readonly JsonSerializerSettings InternalAndPublicMembers = CreateSettings(new InternalMembersContractResolver());
   public static readonly JsonSerializerSettings PublicMembers = CreateSettings(new PublicMembersContractResolver());

   public static JsonSerializerSettings CreateSettingsWithExclusions(JsonSerializerSettings baseSettings, IReadOnlySet<MemberInfo> excludedMembers)
   {
      var baseResolver = baseSettings.ContractResolver as MemberFilteringContractResolver
                      ?? throw new ArgumentException("Base settings IMust have a MemberFilteringContractResolver", nameof(baseSettings));
      var excludingResolver = new ExcludingMembersContractResolver(baseResolver, excludedMembers);

      var settings = new JsonSerializerSettings(baseSettings)
                     {
                        ContractResolver = excludingResolver
                     };

      return settings;
   }

   static JsonSerializerSettings CreateSettings(IContractResolver resolver) =>
      new()
      {
         TypeNameHandling = TypeNameHandling.All,
         Formatting = Formatting.Indented,
         ContractResolver = resolver,
         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
         MaxDepth = 32,
         Converters =
         {
            new DeterministicOrderedForUnorderedCollectionsConverter(),
            new ExceptionJsonConverter(),
            new StringEnumConverter()
         }
      };
}
