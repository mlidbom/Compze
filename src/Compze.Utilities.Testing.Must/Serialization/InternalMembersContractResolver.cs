using System.Reflection;

namespace Compze.Utilities.Testing.Must.Serialization;

internal class InternalMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldInclude(PropertyInfo property) =>
      property.GetMethod is { IsPublic: true } or { IsAssembly: true } or { IsFamilyOrAssembly: true } or { IsFamily: true };

   protected override bool ShouldInclude(FieldInfo field) =>
      field.IsPublic || field.IsAssembly || field.IsFamilyOrAssembly || field.IsFamily;
}
