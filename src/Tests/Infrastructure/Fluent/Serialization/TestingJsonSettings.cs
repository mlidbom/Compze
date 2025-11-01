using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

#pragma warning disable CA2326 // TypeNameHandling is safe for testing serialization
#pragma warning disable CA2327

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

static class TestingJsonSettings
{
   internal static readonly JsonSerializerSettings AllMembers = CreateSettings(new AllMembersContractResolver());
   internal static readonly JsonSerializerSettings InternalAndPublicMembers = CreateSettings(new InternalMembersContractResolver());
   internal static readonly JsonSerializerSettings PublicMembers = CreateSettings(new PublicMembersContractResolver());

   internal static JsonSerializerSettings CreateSettingsWithExclusions(JsonSerializerSettings baseSettings, IReadOnlySet<MemberInfo> excludedMembers)
   {
      var baseResolver = baseSettings.ContractResolver as MemberFilteringContractResolver
                      ?? throw new ArgumentException("Base settings must have a MemberFilteringContractResolver", nameof(baseSettings));
      var excludingResolver = new ExcludingMembersContractResolver(baseResolver, excludedMembers);

      var settings = new JsonSerializerSettings
                     {
                        TypeNameHandling = baseSettings.TypeNameHandling,
                        TypeNameAssemblyFormatHandling = baseSettings.TypeNameAssemblyFormatHandling,
                        Formatting = baseSettings.Formatting,
                        ContractResolver = excludingResolver,
                        ReferenceLoopHandling = baseSettings.ReferenceLoopHandling,
                        MaxDepth = baseSettings.MaxDepth
                     };

      return settings;
   }

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
