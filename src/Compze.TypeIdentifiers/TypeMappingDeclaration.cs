namespace Compze.TypeIdentifiers;

/// <summary>
/// Assembly-level attribute that points to a class implementing <see cref="ITypeMappingDeclaration"/>.
/// Each assembly that has mapped types should declare exactly one of these.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class TypeMappingsAttribute(Type declarationType) : Attribute
{
   /// <summary>The type that implements <see cref="ITypeMappingDeclaration"/>.</summary>
   public Type DeclarationType { get; } = declarationType;
}

/// <summary>
/// Implemented by each assembly's mapping declaration class.
/// Called at registration time to collect type↔GUID mappings.
/// </summary>
public interface ITypeMappingDeclaration
{
   void DeclareMappings(ITypeMappingRegistrar registrar);
}

/// <summary>
/// Fluent interface for declaring type↔GUID mappings within an <see cref="ITypeMappingDeclaration"/>.
/// </summary>
public interface ITypeMappingRegistrar
{
   /// <summary>Map a concrete leaf type to a GUID.</summary>
   ITypeMappingRegistrar Map<T>(string id);

   /// <summary>Map an open generic definition (e.g. <c>typeof(MyGeneric&lt;&gt;)</c>) to a GUID.</summary>
   ITypeMappingRegistrar MapOpenGeneric(Type openGenericType, string id);
}
