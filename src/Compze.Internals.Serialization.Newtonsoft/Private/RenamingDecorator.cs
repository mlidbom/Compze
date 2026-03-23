using System.Text.RegularExpressions;
using Compze.TypeIdentifiers;
using Compze.Internals.SystemCE;

namespace Compze.Internals.Serialization.Newtonsoft.Private;

class RenamingDecorator(ITypeMap typeMap)
{
   readonly ITypeMap _typeMap = typeMap;

   static readonly LazyCE<Regex> FindTypeNames = new(() => new Regex("""
                                                                     "\$type"\: "([^"]*)"
                                                                     """,
                                                                     RegexOptions.Compiled));

   public string ReplaceTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeNamesWithPersistedStrings);

   string ReplaceTypeNamesWithPersistedStrings(Match match)
   {
      var type = Type.GetType(match.Groups[1].Value);
      var persistedString = _typeMap.ToPersistedTypeString(type!);
      return $"""
              "$type": "{persistedString}"
              """;
   }

   public string RestoreTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplacePersistedStringsWithTypeNames);

   string ReplacePersistedStringsWithTypeNames(Match match)
   {
      var type = _typeMap.FromPersistedTypeString(match.Groups[1].Value);
      return $"""
              "$type": "{type.AssemblyQualifiedName}"
              """;
   }
}
