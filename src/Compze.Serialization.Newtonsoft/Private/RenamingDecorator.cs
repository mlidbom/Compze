using System;
using System.Text.RegularExpressions;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Utilities.SystemCE;

namespace Compze.Serialization.Newtonsoft.Private;

class RenamingDecorator(ITypeMapper typeMapper)
{
   readonly ITypeMapper _typeMapper = typeMapper;

   static readonly LazyCE<Regex> FindTypeNames = new(() => new Regex("""
                                                                     "\$type"\: "([^"]*)"
                                                                     """,
                                                                     RegexOptions.Compiled));

   public string ReplaceTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeNamesWithTypeIds);

   //urgent: Apparently this code is not executed. We urgently need tests to verify that we actually replace type names in the JSon
   string ReplaceTypeNamesWithTypeIds(Match match)
   {
      var type = Type.GetType(match.Groups[1].Value);
      var typeId = _typeMapper.GetId(type!);
      return $"""
              "$type": "{typeId}"
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
