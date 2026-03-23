using System.Reflection;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Registers .NET type mappings from assemblies.
/// Leaf types get <see cref="MappedTypeIdentifier"/> (GUID-backed). Generic and composite types
/// get structural string representations that combine mapped GUIDs with stable assembly-qualified names.
/// Supports incremental assembly registration.
/// </summary>
public interface ITypeMapper
{
   /// <summary>
   /// Register type mappings from the assembly containing <typeparamref name="T"/>.
   /// The assembly must have a <see cref="AssemblyTypeMapperAttribute"/> with an <see cref="IAssemblyTypeMapper"/>.
   /// </summary>
   void MapTypesFromAssemblyContaining<T>();

   /// <summary>Register type mappings from the specified assembly.</summary>
   void MapTypesFromAssembly(Assembly assembly);

   /// <summary>Register the assembly containing <typeparamref name="T"/> as stable (type names pass through unchanged).</summary>
   void UseStableNameStrategyForAssemblyContaining<T>();
}
