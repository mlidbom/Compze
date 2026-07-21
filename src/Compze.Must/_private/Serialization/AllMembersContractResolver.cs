using System.Reflection;

namespace Compze.Must._private.Serialization;

class AllMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldInclude(PropertyInfo property) => true;
   protected override bool ShouldInclude(FieldInfo field) => true;
}
