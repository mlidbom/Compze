using Compze.TypeIdentifiers;
using Compze.Must;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

public class TypeNameParser_specification
{
   static TypeNameParser.ParsedTypeName Parse(string input) => TypeNameParser.Parse(input);

   public class parsing_a_leaf_type : TypeNameParser_specification
   {
      [XF] public void parses_type_name()
         => Parse("MyNamespace.MyType, MyAssembly").TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void parses_assembly_name()
         => Parse("MyNamespace.MyType, MyAssembly").AssemblyName.Must().Be("MyAssembly");

      [XF] public void has_no_type_arguments()
         => Parse("MyNamespace.MyType, MyAssembly").TypeArguments.Must().BeNull();

      [XF] public void round_trips_to_original_string()
         => Parse("MyNamespace.MyType, MyAssembly").ToAssemblyQualifiedNameString()
               .Must().Be("MyNamespace.MyType, MyAssembly");
   }

   public class parsing_a_mapped_leaf_type : TypeNameParser_specification
   {
      const string MappedTypeString = "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0";

      [XF] public void parses_guid_as_type_name()
         => Parse(MappedTypeString).TypeName.Must().Be("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");

      [XF] public void parses_zero_as_assembly_name()
         => Parse(MappedTypeString).AssemblyName.Must().Be("0");

      [XF] public void round_trips()    
         => Parse(MappedTypeString).ToAssemblyQualifiedNameString()
               .Must().Be(MappedTypeString);
   }

   public class parsing_a_simple_generic : TypeNameParser_specification
   {
      const string GenericString = "System.Collections.Generic.List`1[[MyNamespace.MyType, MyAssembly]], System.Private.CoreLib";

      [XF] public void parses_generic_type_name_with_arity()
         => Parse(GenericString).TypeName.Must().Be("System.Collections.Generic.List`1");

      [XF] public void parses_outer_assembly_name()
         => Parse(GenericString).AssemblyName.Must().Be("System.Private.CoreLib");

      [XF] public void has_one_type_argument()
         => Parse(GenericString).TypeArguments!.Length.Must().Be(1);

      [XF] public void type_argument_has_correct_type_name()
         => Parse(GenericString).TypeArguments![0].TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void type_argument_has_correct_assembly_name()
         => Parse(GenericString).TypeArguments![0].AssemblyName.Must().Be("MyAssembly");

      [XF] public void round_trips()
         => Parse(GenericString).ToAssemblyQualifiedNameString()
               .Must().Be(GenericString);
   }

   public class parsing_a_multi_argument_generic : TypeNameParser_specification
   {
      const string DictionaryString = "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Int32, System.Private.CoreLib]], System.Private.CoreLib";

      [XF] public void has_two_type_arguments()
         => Parse(DictionaryString).TypeArguments!.Length.Must().Be(2);

      [XF] public void first_argument_is_String()
         => Parse(DictionaryString).TypeArguments![0].TypeName.Must().Be("System.String");

      [XF] public void second_argument_is_Int32()
         => Parse(DictionaryString).TypeArguments![1].TypeName.Must().Be("System.Int32");

      [XF] public void round_trips()
         => Parse(DictionaryString).ToAssemblyQualifiedNameString()
               .Must().Be(DictionaryString);
   }

   public class parsing_a_generic_with_mapped_argument : TypeNameParser_specification
   {
      const string MixedGenericString = "System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib";

      [XF] public void outer_type_is_stable()
         => Parse(MixedGenericString).AssemblyName.Must().Be("System.Private.CoreLib");

      [XF] public void argument_type_name_is_guid()
         => Parse(MixedGenericString).TypeArguments![0].TypeName.Must().Be("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");

      [XF] public void argument_assembly_is_zero()
         => Parse(MixedGenericString).TypeArguments![0].AssemblyName.Must().Be("0");

      [XF] public void round_trips()
         => Parse(MixedGenericString).ToAssemblyQualifiedNameString()
               .Must().Be(MixedGenericString);
   }

   public class parsing_nested_generics : TypeNameParser_specification
   {
      const string NestedString = "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Collections.Generic.List`1[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], System.Private.CoreLib]], System.Private.CoreLib";

      [XF] public void outer_type_is_Dictionary()
         => Parse(NestedString).TypeName.Must().Be("System.Collections.Generic.Dictionary`2");

      [XF] public void first_argument_is_String()
         => Parse(NestedString).TypeArguments![0].TypeName.Must().Be("System.String");

      [XF] public void second_argument_is_a_generic()
         => Parse(NestedString).TypeArguments![1].TypeArguments!.Length.Must().Be(1);

      [XF] public void second_argument_is_List()
         => Parse(NestedString).TypeArguments![1].TypeName.Must().Be("System.Collections.Generic.List`1");

      [XF] public void nested_argument_is_mapped()
         => Parse(NestedString).TypeArguments![1].TypeArguments![0].AssemblyName.Must().Be("0");

      [XF] public void round_trips()
         => Parse(NestedString).ToAssemblyQualifiedNameString()
               .Must().Be(NestedString);
   }

   public class parsing_an_array : TypeNameParser_specification
   {
      const string ArrayString = "MyNamespace.MyType[], MyAssembly";

      [XF] public void type_name_does_not_include_array_suffix()
         => Parse(ArrayString).TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void array_suffix_is_parsed()
         => Parse(ArrayString).ArraySuffix.Must().Be("[]");

      [XF] public void parses_assembly_name()
         => Parse(ArrayString).AssemblyName.Must().Be("MyAssembly");

      [XF] public void round_trips()
         => Parse(ArrayString).ToAssemblyQualifiedNameString()
               .Must().Be(ArrayString);
   }

   public class parsing_a_multidimensional_array : TypeNameParser_specification
   {
      const string MultiDimArrayString = "MyNamespace.MyType[,], MyAssembly";

      [XF] public void array_suffix_includes_rank()
         => Parse(MultiDimArrayString).ArraySuffix.Must().Be("[,]");

      [XF] public void round_trips()
         => Parse(MultiDimArrayString).ToAssemblyQualifiedNameString()
               .Must().Be(MultiDimArrayString);
   }

   public class parsing_a_mapped_array : TypeNameParser_specification
   {
      const string MappedArrayString = "e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c[], 0";

      [XF] public void type_name_is_guid()
         => Parse(MappedArrayString).TypeName.Must().Be("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");

      [XF] public void array_suffix_is_parsed()
         => Parse(MappedArrayString).ArraySuffix.Must().Be("[]");

      [XF] public void assembly_is_zero()
         => Parse(MappedArrayString).AssemblyName.Must().Be("0");

      [XF] public void round_trips()
         => Parse(MappedArrayString).ToAssemblyQualifiedNameString()
               .Must().Be(MappedArrayString);
   }

   public class parsing_an_array_of_generic : TypeNameParser_specification
   {
      const string ArrayOfGenericString = "System.Collections.Generic.List`1[[MyNamespace.MyType, MyAssembly]][], System.Private.CoreLib";

      [XF] public void type_name_has_arity_without_array_suffix()
         => Parse(ArrayOfGenericString).TypeName.Must().Be("System.Collections.Generic.List`1");

      [XF] public void array_suffix_is_parsed()
         => Parse(ArrayOfGenericString).ArraySuffix.Must().Be("[]");

      [XF] public void has_one_type_argument()
         => Parse(ArrayOfGenericString).TypeArguments!.Length.Must().Be(1);

      [XF] public void round_trips()
         => Parse(ArrayOfGenericString).ToAssemblyQualifiedNameString()
               .Must().Be(ArrayOfGenericString);
   }

   public class parsing_a_generic_with_array_argument : TypeNameParser_specification
   {
      const string GenericWithArrayArgString = "System.Collections.Generic.List`1[[MyNamespace.MyType[], MyAssembly]], System.Private.CoreLib";

      [XF] public void argument_array_suffix_is_parsed()
         => Parse(GenericWithArrayArgString).TypeArguments![0].ArraySuffix.Must().Be("[]");

      [XF] public void argument_type_name_does_not_include_array_suffix()
         => Parse(GenericWithArrayArgString).TypeArguments![0].TypeName.Must().Be("MyNamespace.MyType");

      [XF] public void round_trips()
         => Parse(GenericWithArrayArgString).ToAssemblyQualifiedNameString()
               .Must().Be(GenericWithArrayArgString);
   }

   public class parsing_a_mapped_generic_definition : TypeNameParser_specification
   {
      const string MappedGenericString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890[[e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c, 0]], 0";

      [XF] public void type_name_is_guid()
         => Parse(MappedGenericString).TypeName.Must().Be("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

      [XF] public void assembly_is_zero()
         => Parse(MappedGenericString).AssemblyName.Must().Be("0");

      [XF] public void has_one_type_argument()
         => Parse(MappedGenericString).TypeArguments!.Length.Must().Be(1);

      [XF] public void argument_is_mapped()
         => Parse(MappedGenericString).TypeArguments![0].AssemblyName.Must().Be("0");

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
