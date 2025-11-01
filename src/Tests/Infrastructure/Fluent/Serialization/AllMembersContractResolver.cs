using System.Reflection;

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

class AllMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldIncludeProperty(PropertyInfo property) => true;
   protected override bool ShouldIncludeField(FieldInfo field) => true;
}
