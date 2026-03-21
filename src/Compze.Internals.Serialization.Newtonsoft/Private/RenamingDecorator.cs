using System.Text.RegularExpressions;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Internals.SystemCE;

namespace Compze.Internals.Serialization.Newtonsoft.Private;

class RenamingDecorator(IStructuralTypeMapper typeMapper)
{
   readonly IStructuralTypeMapper _typeMapper = typeMapper;

   static readonly LazyCE<Regex> FindTypeNames = new(() => new Regex("""
                                                                     "\$type"\: "([^"]*)"
                                                                     """,
                                                                     RegexOptions.Compiled));

   public string ReplaceTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeNamesWithPersistedStrings);

   string ReplaceTypeNamesWithPersistedStrings(Match match)
   {
      var type = Type.GetType(match.Groups[1].Value);
      var persistedString = _typeMapper.ToPersistedTypeString(type!);
      return $"""
              "$type": "{persistedString}"
              """;
   }

   public string RestoreTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplacePersistedStringsWithTypeNames);

   string ReplacePersistedStringsWithTypeNames(Match match)
   {
      var type = _typeMapper.FromPersistedTypeString(match.Groups[1].Value);
      return $"""
              "$type": "{type.AssemblyQualifiedName}"
              """;
   }
}
