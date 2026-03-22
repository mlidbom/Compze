using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.Parsing;
using Compze.Must;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

public class TypeNameParser_specification
{
   static ParsedTypeName Parse(string input) => ParsedTypeName.Parse(input);
   static ParsedLeafTypeName ParseLeaf(string input) => (ParsedLeafTypeName)Parse(input);
   static ParsedMappedLeafTypeName ParseMappedLeaf(string input) => (ParsedMappedLeafTypeName)Parse(input);
   static ParsedGenericTypeName ParseGeneric(string input) => (ParsedGenericTypeName)Parse(input);
   static ParsedMappedGenericTypeName ParseMappedGeneric(string input) => (ParsedMappedGenericTypeName)Parse(input);
   static ParsedArrayTypeName ParseArray(string input) => (ParsedArrayTypeName)Parse(input);

   public class parsing_a_leaf_type : TypeNameParser_specification
   {
      [XF] public void parses_type_name()
         => ParseLeaf("MyNamespace.MyType, MyAssembly").TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void parses_assembly_name()
         => ParseLeaf("MyNamespace.MyType, MyAssembly").AssemblyName.Must().Be("MyAssembly");

      [XF] public void parses_as_leaf_type()
         => (Parse("MyNamespace.MyType, MyAssembly") is ParsedLeafTypeName).Must().Be(true);

      [XF] public void round_trips_to_original_string()
         => Parse("MyNamespace.MyType, MyAssembly").ToAssemblyQualifiedNameString()
               .Must().Be("MyNamespace.MyType, MyAssembly");
   }

   public class parsing_a_mapped_leaf_type : TypeNameParser_specification
   {
      const string MappedTypeString = "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0";

      [XF] public void parses_as_mapped_leaf()
         => (Parse(MappedTypeString) is ParsedMappedLeafTypeName).Must().Be(true);

      [XF] public void parses_guid()
         => ParseMappedLeaf(MappedTypeString).Guid.Must().Be(Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"));

      [XF] public void round_trips()
         => Parse(MappedTypeString).ToAssemblyQualifiedNameString()
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
         => ((ParsedLeafTypeName)ParseGeneric(GenericString).TypeArguments[0]).TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void type_argument_has_correct_assembly_name()
         => ((ParsedLeafTypeName)ParseGeneric(GenericString).TypeArguments[0]).AssemblyName.Must().Be("MyAssembly");

      [XF] public void round_trips()
         => Parse(GenericString).ToAssemblyQualifiedNameString()
               .Must().Be(GenericString);
   }

   public class parsing_a_multi_argument_generic : TypeNameParser_specification
   {
      const string DictionaryString = "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Int32, System.Private.CoreLib]], System.Private.CoreLib";

      [XF] public void has_two_type_arguments()
         => ParseGeneric(DictionaryString).TypeArguments.Length.Must().Be(2);

      [XF] public void first_argument_is_String()
         => ((ParsedLeafTypeName)ParseGeneric(DictionaryString).TypeArguments[0]).TypeName.Must().Be("System.String");

      [XF] public void second_argument_is_Int32()
         => ((ParsedLeafTypeName)ParseGeneric(DictionaryString).TypeArguments[1]).TypeName.Must().Be("System.Int32");

      [XF] public void round_trips()
         => Parse(DictionaryString).ToAssemblyQualifiedNameString()
               .Must().Be(DictionaryString);
   }

   public class parsing_a_generic_with_mapped_argument : TypeNameParser_specification
   {
      const string MixedGenericString = "System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib";

      [XF] public void outer_type_is_stable()
         => ParseGeneric(MixedGenericString).AssemblyName.Must().Be("System.Private.CoreLib");

      [XF] public void argument_is_mapped_leaf()
         => (ParseGeneric(MixedGenericString).TypeArguments[0] is ParsedMappedLeafTypeName).Must().Be(true);

      [XF] public void argument_guid_is_correct()
         => ((ParsedMappedLeafTypeName)ParseGeneric(MixedGenericString).TypeArguments[0]).Guid.Must().Be(Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"));

      [XF] public void round_trips()
         => Parse(MixedGenericString).ToAssemblyQualifiedNameString()
               .Must().Be(MixedGenericString);
   }

   public class parsing_nested_generics : TypeNameParser_specification
   {
      const string NestedString = "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib]], System.Private.CoreLib";

      [XF] public void outer_type_is_Dictionary()
         => ParseGeneric(NestedString).TypeName.Must().Be("System.Collections.Generic.Dictionary`2");

      [XF] public void first_argument_is_String()
         => ((ParsedLeafTypeName)ParseGeneric(NestedString).TypeArguments[0]).TypeName.Must().Be("System.String");

      [XF] public void second_argument_is_a_generic()
         => ((ParsedGenericTypeName)ParseGeneric(NestedString).TypeArguments[1]).TypeArguments.Length.Must().Be(1);

      [XF] public void second_argument_is_List()
         => ((ParsedGenericTypeName)ParseGeneric(NestedString).TypeArguments[1]).TypeName.Must().Be("System.Collections.Generic.List`1");

      [XF] public void nested_argument_is_mapped_leaf()
         => (((ParsedGenericTypeName)ParseGeneric(NestedString).TypeArguments[1]).TypeArguments[0] is ParsedMappedLeafTypeName).Must().Be(true);

      [XF] public void round_trips()
         => Parse(NestedString).ToAssemblyQualifiedNameString()
               .Must().Be(NestedString);
   }

   public class parsing_an_array : TypeNameParser_specification
   {
      const string ArrayString = "MyNamespace.MyType[], MyAssembly";

      [XF] public void parses_as_array_type()
         => (Parse(ArrayString) is ParsedArrayTypeName).Must().Be(true);

      [XF] public void rank_is_one()
         => ParseArray(ArrayString).Rank.Must().Be(1);

      [XF] public void element_is_leaf()
         => (ParseArray(ArrayString).Element is ParsedLeafTypeName).Must().Be(true);

      [XF] public void element_type_name()
         => ((ParsedLeafTypeName)ParseArray(ArrayString).Element).TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void element_assembly_name()
         => ((ParsedLeafTypeName)ParseArray(ArrayString).Element).AssemblyName.Must().Be("MyAssembly");

      [XF] public void round_trips()
         => Parse(ArrayString).ToAssemblyQualifiedNameString()
               .Must().Be(ArrayString);
   }

   public class parsing_a_multidimensional_array : TypeNameParser_specification
   {
      const string MultiDimArrayString = "MyNamespace.MyType[,], MyAssembly";

      [XF] public void rank_is_two()
         => ParseArray(MultiDimArrayString).Rank.Must().Be(2);

      [XF] public void round_trips()
         => Parse(MultiDimArrayString).ToAssemblyQualifiedNameString()
               .Must().Be(MultiDimArrayString);
   }

   public class parsing_a_mapped_array : TypeNameParser_specification
   {
      const string MappedArrayString = "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c[], 0";

      [XF] public void parses_as_array()
         => (Parse(MappedArrayString) is ParsedArrayTypeName).Must().Be(true);

      [XF] public void element_is_mapped_leaf()
         => (ParseArray(MappedArrayString).Element is ParsedMappedLeafTypeName).Must().Be(true);

      [XF] public void element_guid()
         => ((ParsedMappedLeafTypeName)ParseArray(MappedArrayString).Element).Guid.Must().Be(Guid.Parse("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c"));

      [XF] public void rank_is_one()
         => ParseArray(MappedArrayString).Rank.Must().Be(1);

      [XF] public void round_trips()
         => Parse(MappedArrayString).ToAssemblyQualifiedNameString()
               .Must().Be(MappedArrayString);
   }

   public class parsing_an_array_of_generic : TypeNameParser_specification
   {
      const string ArrayOfGenericString = "System.Collections.Generic.List`1[[MyNamespace.MyType, MyAssembly]][], System.Private.CoreLib";

      [XF] public void parses_as_array()
         => (Parse(ArrayOfGenericString) is ParsedArrayTypeName).Must().Be(true);

      [XF] public void element_is_generic()
         => (ParseArray(ArrayOfGenericString).Element is ParsedGenericTypeName).Must().Be(true);

      [XF] public void element_type_name_has_arity()
         => ((ParsedGenericTypeName)ParseArray(ArrayOfGenericString).Element).TypeName.Must().Be("System.Collections.Generic.List`1");

      [XF] public void element_has_one_type_argument()
         => ((ParsedGenericTypeName)ParseArray(ArrayOfGenericString).Element).TypeArguments.Length.Must().Be(1);

      [XF] public void round_trips()
         => Parse(ArrayOfGenericString).ToAssemblyQualifiedNameString()
               .Must().Be(ArrayOfGenericString);
   }

   public class parsing_a_generic_with_array_argument : TypeNameParser_specification
   {
      const string GenericWithArrayArgString = "System.Collections.Generic.List`1[[MyNamespace.MyType[], MyAssembly]], System.Private.CoreLib";

      [XF] public void argument_is_array()
         => (ParseGeneric(GenericWithArrayArgString).TypeArguments[0] is ParsedArrayTypeName).Must().Be(true);

      [XF] public void argument_array_rank_is_one()
         => ((ParsedArrayTypeName)ParseGeneric(GenericWithArrayArgString).TypeArguments[0]).Rank.Must().Be(1);

      [XF] public void argument_element_type_name()
         => ((ParsedLeafTypeName)((ParsedArrayTypeName)ParseGeneric(GenericWithArrayArgString).TypeArguments[0]).Element).TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void round_trips()
         => Parse(GenericWithArrayArgString).ToAssemblyQualifiedNameString()
               .Must().Be(GenericWithArrayArgString);
   }

   public class parsing_a_mapped_generic_definition : TypeNameParser_specification
   {
      const string MappedGenericString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], 0";

      [XF] public void parses_as_mapped_generic()
         => (Parse(MappedGenericString) is ParsedMappedGenericTypeName).Must().Be(true);

      [XF] public void guid_is_correct()
         => ParseMappedGeneric(MappedGenericString).Guid.Must().Be(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

      [XF] public void has_one_type_argument()
         => ParseMappedGeneric(MappedGenericString).TypeArguments.Length.Must().Be(1);

      [XF] public void argument_is_mapped_leaf()
         => (ParseMappedGeneric(MappedGenericString).TypeArguments[0] is ParsedMappedLeafTypeName).Must().Be(true);

      [XF] public void round_trips()
         => Parse(MappedGenericString).ToAssemblyQualifiedNameString()
               .Must().Be(MappedGenericString);
   }

   public class round_tripping_real_dotnet_types : TypeNameParser_specification
   {
      [XF] public void round_trips_System_String()
      {
         var aqn = typeof(string).AssemblyQualifiedName!;
         Parse(aqn).ToAssemblyQualifiedNameString().Must().Be(aqn);
      }

      [XF] public void round_trips_List_of_string()
      {
         var aqn = typeof(List<string>).AssemblyQualifiedName!;
         Parse(aqn).ToAssemblyQualifiedNameString().Must().Be(aqn);
      }

      [XF] public void round_trips_Dictionary_of_string_int()
      {
         var aqn = typeof(Dictionary<string, int>).AssemblyQualifiedName!;
         Parse(aqn).ToAssemblyQualifiedNameString().Must().Be(aqn);
      }

      [XF] public void round_trips_array_of_int()
      {
         var aqn = typeof(int[]).AssemblyQualifiedName!;
         Parse(aqn).ToAssemblyQualifiedNameString().Must().Be(aqn);
      }

      [XF] public void round_trips_Dictionary_of_string_List_of_int()
      {
         var aqn = typeof(Dictionary<string, List<int>>).AssemblyQualifiedName!;
         Parse(aqn).ToAssemblyQualifiedNameString().Must().Be(aqn);
      }

      [XF] public void round_trips_array_of_List_of_string()
      {
         var aqn = typeof(List<string>[]).AssemblyQualifiedName!;
         Parse(aqn).ToAssemblyQualifiedNameString().Must().Be(aqn);
      }

      [XF] public void round_trips_List_of_int_array()
      {
         var aqn = typeof(List<int[]>).AssemblyQualifiedName!;
         Parse(aqn).ToAssemblyQualifiedNameString().Must().Be(aqn);
      }

      [XF] public void round_trips_multidimensional_array()
      {
         var aqn = typeof(int[,]).AssemblyQualifiedName!;
         Parse(aqn).ToAssemblyQualifiedNameString().Must().Be(aqn);
      }
   }
}
