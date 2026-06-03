namespace Compze.TypeIdentifiers;

/// <summary>
/// Assembly-level attribute that points to a class implementing <see cref="IAssemblyTypeMapper"/>.
/// Each assembly that has mapped types should declare exactly one of these.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AssemblyTypeMapperAttribute(Type mapper) : Attribute
{
   /// <summary>The type that implements <see cref="IAssemblyTypeMapper"/>.</summary>
#pragma warning disable CA1019 // Mapper is intentionally internal: consumed only by the framework's assembly scanning, not part of the public API surface.
   internal Type Mapper { get; } = mapper;
#pragma warning restore CA1019
}
