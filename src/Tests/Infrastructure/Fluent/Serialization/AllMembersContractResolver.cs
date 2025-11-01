using System.Reflection;

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

class AllMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldInclude(PropertyInfo property) => true;
   protected override bool ShouldInclude(FieldInfo field) => true;
}
