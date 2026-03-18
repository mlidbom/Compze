using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>Wraps a <see cref="System.Type"/> with type-mapping classification.
/// Each instance knows its <see cref="TypeId"/> — assigned from explicit mappings for
/// <see cref="ExplicitlyMappedType"/>s, computed deterministically for <see cref="ComputedTypeIdType"/>s.
/// Use <see cref="GetOrCreate"/> to obtain instances — it classifies structurally and resolves TypeIds.</summary>
abstract class TypeMapperType
{
   internal Type Type { get; }
   internal abstract TypeId? TypeId { get; }
   internal string FullNameCompilable => Type.GetFullNameCompilable();

   TypeMapperType(Type type) => Type = type;

   /// <summary>Creates or retrieves a <see cref="TypeMapperType"/> for <paramref name="type"/>.
   /// Recursively creates component types (type arguments, element types, generic definitions).
   /// <paramref name="resolveExplicitTypeId"/> provides hand-assigned TypeIds from mapping files.</summary>
   internal static TypeMapperType GetOrCreate(Type type, Func<Type, TypeId?> resolveExplicitTypeId, Dictionary<Type, TypeMapperType> cache)
   {
      if(cache.TryGetValue(type, out var existing)) return existing;

      TypeMapperType result;

      if(type.IsArray)
      {
         var elementType = GetOrCreate(type.GetElementType()!, resolveExplicitTypeId, cache);
         result = new ArrayType(type, elementType);
      }
      else if(type is { IsGenericType: true, IsGenericTypeDefinition: false })
      {
         var definition = (OpenGenericDefinition)GetOrCreate(type.GetGenericTypeDefinition(), resolveExplicitTypeId, cache);
         var typeArguments = type.GetGenericArguments()
                                .Select(arg => GetOrCreate(arg, resolveExplicitTypeId, cache))
                                .ToArray();
         result = new ClosedGenericType(type, definition, typeArguments);
      }
      else if(type.IsGenericTypeDefinition)
      {
         result = new OpenGenericDefinition(type, resolveExplicitTypeId(type));
      }
      else
      {
         result = new LeafType(type, resolveExplicitTypeId(type));
      }

      cache[type] = result;
      return result;
   }

   /// <summary>Needs a hand-assigned GUID in the mapping file.</summary>
   internal abstract class ExplicitlyMappedType : TypeMapperType
   {
      readonly TypeId? _typeId;
      internal override TypeId? TypeId => _typeId;

      protected ExplicitlyMappedType(Type type, TypeId? typeId) : base(type) => _typeId = typeId;
   }

   /// <summary>Non-generic, non-array type (concrete class, abstract event interface, etc.).</summary>
   internal sealed class LeafType(Type type, TypeId? typeId) : ExplicitlyMappedType(type, typeId);

   /// <summary>An open generic definition such as <c>List&lt;&gt;</c> or <c>Dictionary&lt;,&gt;</c>.</summary>
   internal sealed class OpenGenericDefinition(Type type, TypeId? typeId) : ExplicitlyMappedType(type, typeId);

   /// <summary>TypeId computed deterministically from component TypeIds.</summary>
   internal abstract class ComputedTypeIdType(Type type) : TypeMapperType(type);

   /// <summary>A closed generic such as <c>List&lt;MyEntity&gt;</c>.
   /// Holds its <see cref="Definition"/> and <see cref="TypeArguments"/> as fully typed references.
   /// TypeId is computed from the components' TypeIds at construction time.</summary>
   internal sealed class ClosedGenericType : ComputedTypeIdType
   {
      readonly TypeId? _typeId;
      internal override TypeId? TypeId => _typeId;
      internal OpenGenericDefinition Definition { get; }
      internal IReadOnlyList<TypeMapperType> TypeArguments { get; }

      internal ClosedGenericType(Type type, OpenGenericDefinition definition, IReadOnlyList<TypeMapperType> typeArguments) : base(type)
      {
         Definition = definition;
         TypeArguments = typeArguments;
         _typeId = ComputeTypeId();
      }

      TypeId? ComputeTypeId()
      {
         if(Definition.TypeId == null) return null;

         var argumentTypeIds = new TypeId[TypeArguments.Count];
         for(var i = 0; i < TypeArguments.Count; i++)
         {
            if(TypeArguments[i].TypeId == null) return null;
            argumentTypeIds[i] = TypeArguments[i].TypeId!;
         }

         return DeterministicTypeIdGenerator.ComputeCompositeTypeId(Definition.TypeId, argumentTypeIds);
      }
   }

   /// <summary>An array type such as <c>MyEntity[]</c>.
   /// Holds its <see cref="ElementType"/> as a fully typed reference.
   /// TypeId is computed from the element's TypeId at construction time.</summary>
   internal sealed class ArrayType : ComputedTypeIdType
   {
      readonly TypeId? _typeId;
      internal override TypeId? TypeId => _typeId;
      internal TypeMapperType ElementType { get; }

      internal ArrayType(Type type, TypeMapperType elementType) : base(type)
      {
         ElementType = elementType;
         _typeId = ElementType.TypeId != null
            ? DeterministicTypeIdGenerator.ComputeCompositeTypeId(DeterministicTypeIdGenerator.ArrayMarkerTypeId, ElementType.TypeId)
            : null;
      }
   }
}
