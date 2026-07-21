using Compze.Must;

using Compze.xUnitBDD;
using Compze.TypeIdentifiers.Internal;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

public class TypeNameParser_specification
{
   static TypeIdentifier Parse(string input) => TypeIdentifier.Parse(input);
   static StableLeafTypeIdentifier ParseLeaf(string input) => (StableLeafTypeIdentifier)Parse(input);
   static MappedTypeIdentifier ParseMappedLeaf(string input) => (MappedTypeIdentifier)Parse(input);
   static StableGenericTypeIdentifier ParseGeneric(string input) => (StableGenericTypeIdentifier)Parse(input);
   static MappedGenericTypeIdentifier ParseMappedGeneric(string input) => (MappedGenericTypeIdentifier)Parse(input);
   static ArrayTypeIdentifier ParseArray(string input) => (ArrayTypeIdentifier)Parse(input);

   public class parsing_a_leaf_type : TypeNameParser_specification
   {
      [XF] public void parses_type_name()
         => ParseLeaf("MyNamespace.MyType, MyAssembly").TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void parses_assembly_name()
         => ParseLeaf("MyNamespace.MyType, MyAssembly").AssemblyName.Must().Be("MyAssembly");

      [XF] public void parses_as_leaf_type()
         => (Parse("MyNamespace.MyType, MyAssembly") is StableLeafTypeIdentifier).Must().Be(true);

      [XF] public void round_trips_to_original_string()
         => Parse("MyNamespace.MyType, MyAssembly").StringRepresentation
               .Must().Be("MyNamespace.MyType, MyAssembly");
   }

   public class parsing_a_mapped_leaf_type : TypeNameParser_specification
   {
      const string MappedTypeString = "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0";

      [XF] public void parses_as_mapped_leaf()
         => (Parse(MappedTypeString) is MappedTypeIdentifier).Must().Be(true);

      [XF] public void parses_guid()
         => ParseMappedLeaf(MappedTypeString).GuidValue.Must().Be(Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"));

      [XF] public void round_trips()
         => Parse(MappedTypeString).StringRepresentation
               .Must().Be(MappedTypeString);
   }

   public class parsing_a_simple_generic : TypeNameParser_specification
   {
      const string GenericString = "System.Collections.Generic.List`1[[MyNamespace.MyType, MyAssembly]], System.Private.CoreLib";

      [XF] public void parses_generic_type_name_with_arity()
         => ParseGeneric(GenericString).TypeName.Must().Be("System.Collections.Generic.List`1");

      [XF] public void parses_outer_assembly_name()
         => ParseGeneric(GenericString).AssemblyName.Must().Be("System.Private.CoreLib");

      [XF] public void has_one_type_argument()
         => ParseGeneric(GenericString).TypeArguments.Length.Must().Be(1);

      [XF] public void type_argument_has_correct_type_name()
         => ((StableLeafTypeIdentifier)ParseGeneric(GenericString).TypeArguments[0]).TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void type_argument_has_correct_assembly_name()
         => ((StableLeafTypeIdentifier)ParseGeneric(GenericString).TypeArguments[0]).AssemblyName.Must().Be("MyAssembly");

      [XF] public void round_trips()
         => Parse(GenericString).StringRepresentation
               .Must().Be(GenericString);
   }

   public class parsing_a_multi_argument_generic : TypeNameParser_specification
   {
      const string DictionaryString = "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Int32, System.Private.CoreLib]], System.Private.CoreLib";

      [XF] public void has_two_type_arguments()
         => ParseGeneric(DictionaryString).TypeArguments.Length.Must().Be(2);

      [XF] public void first_argument_is_String()
         => ((StableLeafTypeIdentifier)ParseGeneric(DictionaryString).TypeArguments[0]).TypeName.Must().Be("System.String");

      [XF] public void second_argument_is_Int32()
         => ((StableLeafTypeIdentifier)ParseGeneric(DictionaryString).TypeArguments[1]).TypeName.Must().Be("System.Int32");

      [XF] public void round_trips()
         => Parse(DictionaryString).StringRepresentation
               .Must().Be(DictionaryString);
   }

   public class parsing_a_generic_with_mapped_argument : TypeNameParser_specification
   {
      const string MixedGenericString = "System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib";

      [XF] public void outer_type_is_stable()
         => ParseGeneric(MixedGenericString).AssemblyName.Must().Be("System.Private.CoreLib");

      [XF] public void argument_is_mapped_leaf()
         => (ParseGeneric(MixedGenericString).TypeArguments[0] is MappedTypeIdentifier).Must().Be(true);

      [XF] public void argument_guid_is_correct()
         => ((MappedTypeIdentifier)ParseGeneric(MixedGenericString).TypeArguments[0]).GuidValue.Must().Be(Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"));

      [XF] public void round_trips()
         => Parse(MixedGenericString).StringRepresentation
               .Must().Be(MixedGenericString);
   }

   public class parsing_nested_generics : TypeNameParser_specification
   {
      const string NestedString = "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib]], System.Private.CoreLib";

      [XF] public void outer_type_is_Dictionary()
         => ParseGeneric(NestedString).TypeName.Must().Be("System.Collections.Generic.Dictionary`2");

      [XF] public void first_argument_is_String()
         => ((StableLeafTypeIdentifier)ParseGeneric(NestedString).TypeArguments[0]).TypeName.Must().Be("System.String");

      [XF] public void second_argument_is_a_generic()
         => ((StableGenericTypeIdentifier)ParseGeneric(NestedString).TypeArguments[1]).TypeArguments.Length.Must().Be(1);

      [XF] public void second_argument_is_List()
         => ((StableGenericTypeIdentifier)ParseGeneric(NestedString).TypeArguments[1]).TypeName.Must().Be("System.Collections.Generic.List`1");

      [XF] public void nested_argument_is_mapped_leaf()
         => (((StableGenericTypeIdentifier)ParseGeneric(NestedString).TypeArguments[1]).TypeArguments[0] is MappedTypeIdentifier).Must().Be(true);

      [XF] public void round_trips()
         => Parse(NestedString).StringRepresentation
               .Must().Be(NestedString);
   }

   public class parsing_an_array : TypeNameParser_specification
   {
      const string ArrayString = "MyNamespace.MyType[], MyAssembly";

      [XF] public void parses_as_array_type()
         => (Parse(ArrayString) is ArrayTypeIdentifier).Must().Be(true);

      [XF] public void rank_is_one()
         => ParseArray(ArrayString).Rank.Must().Be(1);

      [XF] public void element_is_leaf()
         => (ParseArray(ArrayString).Element is StableLeafTypeIdentifier).Must().Be(true);

      [XF] public void element_type_name()
         => ((StableLeafTypeIdentifier)ParseArray(ArrayString).Element).TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void element_assembly_name()
         => ((StableLeafTypeIdentifier)ParseArray(ArrayString).Element).AssemblyName.Must().Be("MyAssembly");

      [XF] public void round_trips()
         => Parse(ArrayString).StringRepresentation
               .Must().Be(ArrayString);
   }

   public class parsing_a_multidimensional_array : TypeNameParser_specification
   {
      const string MultiDimArrayString = "MyNamespace.MyType[,], MyAssembly";

      [XF] public void rank_is_two()
         => ParseArray(MultiDimArrayString).Rank.Must().Be(2);

      [XF] public void round_trips()
         => Parse(MultiDimArrayString).StringRepresentation
               .Must().Be(MultiDimArrayString);
   }

   public class parsing_a_mapped_array : TypeNameParser_specification
   {
      const string MappedArrayString = "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c[], 0";

      [XF] public void parses_as_array()
         => (Parse(MappedArrayString) is ArrayTypeIdentifier).Must().Be(true);

      [XF] public void element_is_mapped_leaf()
         => (ParseArray(MappedArrayString).Element is MappedTypeIdentifier).Must().Be(true);

      [XF] public void element_guid()
         => ((MappedTypeIdentifier)ParseArray(MappedArrayString).Element).GuidValue.Must().Be(Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"));

      [XF] public void rank_is_one()
         => ParseArray(MappedArrayString).Rank.Must().Be(1);

      [XF] public void round_trips()
         => Parse(MappedArrayString).StringRepresentation
               .Must().Be(MappedArrayString);
   }

   public class parsing_an_array_of_generic : TypeNameParser_specification
   {
      const string ArrayOfGenericString = "System.Collections.Generic.List`1[[MyNamespace.MyType, MyAssembly]][], System.Private.CoreLib";

      [XF] public void parses_as_array()
         => (Parse(ArrayOfGenericString) is ArrayTypeIdentifier).Must().Be(true);

      [XF] public void element_is_generic()
         => (ParseArray(ArrayOfGenericString).Element is StableGenericTypeIdentifier).Must().Be(true);

      [XF] public void element_type_name_has_arity()
         => ((StableGenericTypeIdentifier)ParseArray(ArrayOfGenericString).Element).TypeName.Must().Be("System.Collections.Generic.List`1");

      [XF] public void element_has_one_type_argument()
         => ((StableGenericTypeIdentifier)ParseArray(ArrayOfGenericString).Element).TypeArguments.Length.Must().Be(1);

      [XF] public void round_trips()
         => Parse(ArrayOfGenericString).StringRepresentation
               .Must().Be(ArrayOfGenericString);
   }

   public class parsing_a_jagged_array_of_a_generic : TypeNameParser_specification
   {
      // A jagged array T[][] is an array whose element type is itself an array (T[]). So the correct
      // parse tree is ArrayTypeIdentifier -> ArrayTypeIdentifier -> the generic, with the generic's
      // type argument preserved as its own identifier so it can be transformed/renamed independently.
      const string JaggedArrayOfGenericString = "System.Collections.Generic.List`1[[MyNamespace.MyType, MyAssembly]][][], System.Private.CoreLib";

      [XF] public void parses_as_an_array()
         => (Parse(JaggedArrayOfGenericString) is ArrayTypeIdentifier).Must().BeTrue();

      [XF] public void the_outer_array_has_rank_one()
         => ParseArray(JaggedArrayOfGenericString).Rank.Must().Be(1);

      [XF] public void the_outer_arrays_element_is_itself_an_array()
         => (ParseArray(JaggedArrayOfGenericString).Element is ArrayTypeIdentifier).Must().BeTrue();

      [XF] public void the_innermost_element_is_the_generic_type()
         => (ParseArray(JaggedArrayOfGenericString).Element is ArrayTypeIdentifier { Element: StableGenericTypeIdentifier }).Must().BeTrue();

      [XF] public void the_generics_type_argument_is_preserved_as_its_own_identifier()
      {
         var isPreserved = ParseArray(JaggedArrayOfGenericString).Element
                              is ArrayTypeIdentifier { Element: StableGenericTypeIdentifier { TypeArguments: [StableLeafTypeIdentifier { TypeName: "MyNamespace.MyType" }] } };
         isPreserved.Must().BeTrue();
      }

      [XF] public void round_trips()
         => Parse(JaggedArrayOfGenericString).StringRepresentation
               .Must().Be(JaggedArrayOfGenericString);
   }

   public class parsing_a_jagged_array_of_mixed_ranks : TypeNameParser_specification
   {
      // The CLR writes the outermost array rank last, so "...List`1[[...]][,][]" is a single-dimensional
      // array (the trailing "[]") whose elements are two-dimensional arrays (the "[,]") of the generic.
      const string MixedRankJaggedString = "System.Collections.Generic.List`1[[MyNamespace.MyType, MyAssembly]][,][], System.Private.CoreLib";

      [XF] public void the_outer_array_has_rank_one()
         => ParseArray(MixedRankJaggedString).Rank.Must().Be(1);

      [XF] public void the_outer_arrays_element_is_a_rank_two_array()
         => (ParseArray(MixedRankJaggedString).Element is ArrayTypeIdentifier { Rank: 2 }).Must().BeTrue();

      [XF] public void the_innermost_element_is_the_generic_type()
         => (ParseArray(MixedRankJaggedString).Element is ArrayTypeIdentifier { Element: StableGenericTypeIdentifier }).Must().BeTrue();

      [XF] public void round_trips()
         => Parse(MixedRankJaggedString).StringRepresentation
               .Must().Be(MixedRankJaggedString);
   }

   public class parsing_a_generic_with_array_argument : TypeNameParser_specification
   {
      const string GenericWithArrayArgString = "System.Collections.Generic.List`1[[MyNamespace.MyType[], MyAssembly]], System.Private.CoreLib";

      [XF] public void argument_is_array()
         => (ParseGeneric(GenericWithArrayArgString).TypeArguments[0] is ArrayTypeIdentifier).Must().Be(true);

      [XF] public void argument_array_rank_is_one()
         => ((ArrayTypeIdentifier)ParseGeneric(GenericWithArrayArgString).TypeArguments[0]).Rank.Must().Be(1);

      [XF] public void argument_element_type_name()
         => ((StableLeafTypeIdentifier)((ArrayTypeIdentifier)ParseGeneric(GenericWithArrayArgString).TypeArguments[0]).Element).TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void round_trips()
         => Parse(GenericWithArrayArgString).StringRepresentation
               .Must().Be(GenericWithArrayArgString);
   }

   public class parsing_a_mapped_generic_definition : TypeNameParser_specification
   {
      const string MappedGenericString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], 0";

      [XF] public void parses_as_mapped_generic()
         => (Parse(MappedGenericString) is MappedGenericTypeIdentifier).Must().Be(true);

      [XF] public void guid_is_correct()
         => ParseMappedGeneric(MappedGenericString).GuidValue.Must().Be(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

      [XF] public void has_one_type_argument()
         => ParseMappedGeneric(MappedGenericString).TypeArguments.Length.Must().Be(1);

      [XF] public void argument_is_mapped_leaf()
         => (ParseMappedGeneric(MappedGenericString).TypeArguments[0] is MappedTypeIdentifier).Must().Be(true);

      [XF] public void round_trips()
         => Parse(MappedGenericString).StringRepresentation
               .Must().Be(MappedGenericString);
   }

   // After normalization-on-read, a real .NET type's full AssemblyQualifiedName normalizes to its SHORT-name
   // canonical string (Version/Culture/PublicKeyToken stripped), so it no longer round-trips byte-for-byte.
   public class normalizing_real_dotnet_types : TypeNameParser_specification
   {
      [XF] public void normalizes_System_String()
         => Parse(typeof(string).AssemblyQualifiedName!).StringRepresentation
               .Must().Be("System.String, System.Private.CoreLib");

      [XF] public void normalizes_List_of_string()
         => Parse(typeof(List<string>).AssemblyQualifiedName!).StringRepresentation
               .Must().Be("System.Collections.Generic.List`1[[System.String, System.Private.CoreLib]], System.Private.CoreLib");

      [XF] public void normalizes_Dictionary_of_string_int()
         => Parse(typeof(Dictionary<string, int>).AssemblyQualifiedName!).StringRepresentation
               .Must().Be("System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Int32, System.Private.CoreLib]], System.Private.CoreLib");

      [XF] public void normalizes_array_of_int()
         => Parse(typeof(int[]).AssemblyQualifiedName!).StringRepresentation
               .Must().Be("System.Int32[], System.Private.CoreLib");

      [XF] public void normalizes_Dictionary_of_string_List_of_int()
         => Parse(typeof(Dictionary<string, List<int>>).AssemblyQualifiedName!).StringRepresentation
               .Must().Be("System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib]], System.Private.CoreLib]], System.Private.CoreLib");

      [XF] public void normalizes_array_of_List_of_string()
         => Parse(typeof(List<string>[]).AssemblyQualifiedName!).StringRepresentation
               .Must().Be("System.Collections.Generic.List`1[[System.String, System.Private.CoreLib]][], System.Private.CoreLib");

      [XF] public void normalizes_List_of_int_array()
         => Parse(typeof(List<int[]>).AssemblyQualifiedName!).StringRepresentation
               .Must().Be("System.Collections.Generic.List`1[[System.Int32[], System.Private.CoreLib]], System.Private.CoreLib");

      [XF] public void normalizes_multidimensional_array()
         => Parse(typeof(int[,]).AssemblyQualifiedName!).StringRepresentation
               .Must().Be("System.Int32[,], System.Private.CoreLib");
   }
}
