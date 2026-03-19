namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>
/// Subclass hierarchy encodes what kind of type this is — leaf, open generic, closed generic, or array.
/// Each subclass exposes its structural components as typed references (not raw <see cref="System.Type"/>).
/// </summary>
abstract class TypeMapperType
{
   internal Type Type { get; }

   TypeMapperType(Type type) => Type = type;

   /// <summary>Classifies a <see cref="System.Type"/> into the correct structural subtype.
   /// Recursively classifies components (type arguments, element types, generic definitions).</summary>
   internal static TypeMapperType FromType(Type type)
   {
      if(type.IsArray)
         return new ArrayType(type, FromType(type.GetElementType()!));

      if(type is { IsGenericType: true, IsGenericTypeDefinition: false })
      {
         return new ClosedGenericType(type,
                                      openGeneric: new OpenGeneric(type.GetGenericTypeDefinition()),
                                      typeArguments: type.GetGenericArguments().Select(FromType).ToArray());
      }

      if(type.IsGenericTypeDefinition)
         return new OpenGeneric(type);

      return new LeafType(type);
   }

   /// <summary>Needs a hand-assigned GUID in the mapping file.</summary>
   internal abstract class ExplicitlyMappedType(Type type) : TypeMapperType(type);

   /// <summary>Non-generic, non-array type (concrete class, abstract event interface, etc.).</summary>
   internal sealed class LeafType(Type type) : ExplicitlyMappedType(type);

   /// <summary>An open generic definition such as <c>List&lt;&gt;</c> or <c>Dictionary&lt;,&gt;</c>.</summary>
   internal sealed class OpenGeneric(Type type) : ExplicitlyMappedType(type);

   /// <summary>TypeId is computed deterministically from component TypeIds — not stored here.</summary>
   internal abstract class ComputedTypeIdType(Type type) : TypeMapperType(type);

   /// <summary>A closed generic such as <c>List&lt;MyEntity&gt;</c>.
   /// <see cref="OpenGenericType"/> is the <see cref="OpenGenericDefinition"/>,
   /// <see cref="TypeArguments"/> are the fully classified component types.</summary>
   internal sealed class ClosedGenericType : ComputedTypeIdType
   {
      internal OpenGeneric OpenGenericType { get; }
      internal IReadOnlyList<TypeMapperType> TypeArguments { get; }

      internal ClosedGenericType(Type type, OpenGeneric openGeneric, IReadOnlyList<TypeMapperType> typeArguments) : base(type)
      {
         OpenGenericType = openGeneric;
         TypeArguments = typeArguments;
      }
   }

   /// <summary>An array type such as <c>MyEntity[]</c>.
   /// <see cref="ElementType"/> is the fully classified element type.</summary>
   internal sealed class ArrayType : ComputedTypeIdType
   {
      internal TypeMapperType ElementType { get; }

      internal ArrayType(Type type, TypeMapperType elementType) : base(type) => ElementType = elementType;
   }
}
