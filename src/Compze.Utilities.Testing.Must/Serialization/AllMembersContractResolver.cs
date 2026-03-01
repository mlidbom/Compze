using System.Reflection;

namespace Compze.Utilities.Testing.Must.Serialization;

internal class AllMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldInclude(PropertyInfo property) => true;
   protected override bool ShouldInclude(FieldInfo field) => true;
}
