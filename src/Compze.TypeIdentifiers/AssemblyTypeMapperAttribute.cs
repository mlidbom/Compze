namespace Compze.TypeIdentifiers;

/// <summary>
/// Assembly-level attribute that points to a class implementing <see cref="IAssemblyTypeMapper"/>.
/// Each assembly that has mapped types should declare exactly one of these.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class AssemblyTypeMapperAttribute(Type mapper) : Attribute
{
   /// <summary>The type that implements <see cref="IAssemblyTypeMapper"/>.</summary>
   public Type Mapper { get; } = mapper;
}
