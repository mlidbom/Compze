using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>Wraps a <see cref="System.Type"/> with the classification needed by the type mapper.
/// Subclass hierarchy encodes the structural distinction between types needing explicit GUID mappings
/// and types whose TypeIds are computed deterministically from their components.</summary>
abstract class TypeMapperType
{
   internal Type Type { get; }

   TypeMapperType(Type type) => Type = type;

   internal string FullNameCompilable => Type.GetFullNameCompilable();

   /// <summary>Needs a hand-assigned GUID in the mapping file.</summary>
   internal abstract class ExplicitlyMappedType(Type type) : TypeMapperType(type);

   /// <summary>Non-generic, non-array type (concrete class, abstract event interface, etc.).</summary>
   internal sealed class LeafType(Type type) : ExplicitlyMappedType(type);

   /// <summary>An open generic definition such as <c>List&lt;&gt;</c> or <c>Dictionary&lt;,&gt;</c>.</summary>
   internal sealed class OpenGenericDefinition(Type type) : ExplicitlyMappedType(type);

   /// <summary>TypeId is computed deterministically from component TypeIds.</summary>
   internal abstract class ComputedTypeIdType(Type type) : TypeMapperType(type);

   /// <summary>A closed generic such as <c>List&lt;MyEntity&gt;</c>.
   /// Components are raw <see cref="System.Type"/>s — the open generic definition and type arguments.</summary>
   internal sealed class ClosedGenericType(Type type) : ComputedTypeIdType(type)
   {
      internal Type GenericDefinition { get; } = type.GetGenericTypeDefinition();
      internal IReadOnlyList<Type> TypeArguments { get; } = type.GetGenericArguments();
   }

   /// <summary>An array type such as <c>MyEntity[]</c>.</summary>
   internal sealed class ArrayType(Type type) : ComputedTypeIdType(type)
   {
      internal Type ElementType { get; } = type.GetElementType()!;
   }
}
