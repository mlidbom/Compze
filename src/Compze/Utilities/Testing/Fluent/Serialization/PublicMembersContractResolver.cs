using System.Reflection;

namespace Compze.Utilities.Testing.Fluent.Serialization;

class PublicMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldInclude(PropertyInfo property) => property.GetMethod?.IsPublic ?? false;
   protected override bool ShouldInclude(FieldInfo field) => field.IsPublic;
}
