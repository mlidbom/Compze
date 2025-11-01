using System.Reflection;

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

class PublicMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldIncludeProperty(PropertyInfo property) => property.GetMethod?.IsPublic ?? false;
   protected override bool ShouldIncludeField(FieldInfo field) => field.IsPublic;
}
