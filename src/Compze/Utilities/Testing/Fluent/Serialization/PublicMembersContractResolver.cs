using System.Reflection;

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

class PublicMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldInclude(PropertyInfo property) => property.GetMethod?.IsPublic ?? false;
   protected override bool ShouldInclude(FieldInfo field) => field.IsPublic;
}
