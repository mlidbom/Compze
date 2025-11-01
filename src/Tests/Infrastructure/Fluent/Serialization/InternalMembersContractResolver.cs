using System.Reflection;

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

class InternalMembersContractResolver : MemberFilteringContractResolver
{
   protected override bool ShouldInclude(PropertyInfo property) =>
      property.GetMethod is { IsAssembly: true } or { IsFamilyOrAssembly: true } or { IsFamily: true };

   protected override bool ShouldInclude(FieldInfo field) =>
      field.IsAssembly || field.IsFamilyOrAssembly || field.IsFamily;
}
