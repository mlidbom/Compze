using System.Reflection;
using Compze.Contracts;

namespace Compze.Must._private.Serialization.Rules;

/// <summary>
/// A member Must leaves out when it serializes a value: the member named <paramref name="name"/> on <paramref name="declaringType"/>,
/// and on every type derived from it.
/// </summary>
class ExcludedMember(Type declaringType, string name)
{
   readonly Type _declaringType = declaringType;
   readonly string _name = name;

   public static ExcludedMember Of(MemberInfo member) => new(member.DeclaringType._assert().NotNull(), member.Name);

   public bool Matches(Type objectType, MemberInfo member) => member.Name == _name && _declaringType.IsAssignableFrom(objectType);
}
