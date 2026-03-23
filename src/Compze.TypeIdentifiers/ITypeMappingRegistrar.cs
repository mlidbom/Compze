namespace Compze.TypeIdentifiers;

/// <summary>
/// Fluent interface for declaring type↔GUID mappings within an <see cref="IAssemblyTypeMapper"/>.
/// </summary>
public interface ITypeMappingRegistrar
{
   /// <summary>Map a concrete leaf type to a GUID.</summary>
   ITypeMappingRegistrar Map<T>(string id);

   /// <summary>Map an open generic definition (e.g. <c>typeof(MyGeneric&lt;&gt;)</c>) to a GUID.</summary>
   ITypeMappingRegistrar MapOpenGeneric(Type openGenericType, string id);
}
