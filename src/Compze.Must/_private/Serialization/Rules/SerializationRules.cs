using System.Collections.Concurrent;
using System.Reflection;
using Compze.Must._private.Serialization.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#pragma warning disable CA2326 // TypeNameHandling is safe here: Must only writes JSON, it never reads any, and its WriteOnlySerializationBinder refuses to resolve a type.
#pragma warning disable CA2327 // As above.

namespace Compze.Must._private.Serialization.Rules;

/// <summary>
/// The rules that change how Must serializes values: members it leaves out, types it writes its own way,
/// and whether it writes type names.
/// </summary>
/// <remarks>
/// An assertion chain's own rules, configured through <see cref="ISerializationConfig{TValue}"/>, take precedence
/// over the global rules of <see cref="MustSerialization"/>: see <see cref="TakingPrecedenceOver"/>.
/// </remarks>
/// <remarks>
/// A type rule is a <see cref="JsonConverter"/> that claims the values of the types it applies to; the first one that
/// does wins. The Newtonsoft settings that realize a set of rules are built once per <see cref="MemberVisibility"/> and
/// cached in the instance, so the contracts Newtonsoft builds per type are reused for as long as the rules are.
/// </remarks>
class SerializationRules
{
   public static readonly SerializationRules None = new([], [], omitsTypeNames: false);

   readonly IReadOnlyList<ExcludedMember> _excludedMembers;
   readonly IReadOnlyList<JsonConverter> _typeRules;
   readonly bool _omitsTypeNames;
   readonly ConcurrentDictionary<MemberVisibility, JsonSerializerSettings> _settingsByVisibility = new();

   SerializationRules(IReadOnlyList<ExcludedMember> excludedMembers, IReadOnlyList<JsonConverter> typeRules, bool omitsTypeNames)
   {
      _excludedMembers = excludedMembers;
      _typeRules = typeRules;
      _omitsTypeNames = omitsTypeNames;
   }

   public SerializationRules WithExcludedMember(ExcludedMember member) => new([.._excludedMembers, member], _typeRules, _omitsTypeNames);
   public SerializationRules WithTypeRule(JsonConverter typeRule) => new(_excludedMembers, [.._typeRules, typeRule], _omitsTypeNames);
   public SerializationRules WithTypeNamesOmitted() => new(_excludedMembers, _typeRules, omitsTypeNames: true);

   /// <summary>These rules first, then <paramref name="globalRules"/>, so that where both have a rule for a type, these win.</summary>
   public SerializationRules TakingPrecedenceOver(SerializationRules globalRules) =>
      this == None
         ? globalRules
         : new SerializationRules([.._excludedMembers, ..globalRules._excludedMembers],
                                  [.._typeRules, ..globalRules._typeRules],
                                  _omitsTypeNames || globalRules._omitsTypeNames);

   public bool Excludes(Type objectType, MemberInfo member) => _excludedMembers.Any(it => it.Matches(objectType, member));

   public JsonConverter? TypeRuleFor(Type type) => _typeRules.FirstOrDefault(it => it.CanConvert(type));

   public JsonSerializerSettings SettingsFor(MemberVisibility visibility) => _settingsByVisibility.GetOrAdd(visibility, CreateSettings);

   JsonSerializerSettings CreateSettings(MemberVisibility visibility) =>
      new()
      {
         TypeNameHandling = _omitsTypeNames ? TypeNameHandling.None : TypeNameHandling.All,
         Formatting = Formatting.Indented,
         ContractResolver = new MustContractResolver(visibility, this),
         SerializationBinder = new WriteOnlySerializationBinder(),
         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
         Converters =
         {
            new DeterministicOrderedForUnorderedCollectionsConverter(),
            new StringEnumConverter()
         }
      };
}
