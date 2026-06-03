using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Internals.SystemCE.Tests.ReflectionCE;

public class GetFullNameCompilable_
{
   // ReSharper disable ClassNeverInstantiated.Local
   // ReSharper disable UnusedTypeParameter Empty marker types, generic only so the specs can exercise open-generic name formatting via typeof(...<>); the parameters are intentionally unused.
#pragma warning disable CA1812 // Test fixture types referenced only via typeof()
   class Nested;
   class GenericNested<T>;
   class TwoParameterGenericNested<T1, T2>;
#pragma warning restore CA1812
   // ReSharper restore UnusedTypeParameter
   // ReSharper restore ClassNeverInstantiated.Local

   public class Non_generic_types
   {
      [XF] public void returns_full_name_for_simple_type() =>
         typeof(string).GetFullNameCompilable().Must().Be("System.String");

      [XF] public void returns_full_name_for_value_type() =>
         typeof(int).GetFullNameCompilable().Must().Be("System.Int32");

      [XF] public void handles_nested_types_by_replacing_plus_with_dot() =>
         typeof(Nested).GetFullNameCompilable().Must().Be("Compze.Internals.SystemCE.Tests.ReflectionCE.GetFullNameCompilable_.Nested");
   }

   public class Array_types
   {
      [XF] public void returns_full_name_with_brackets() =>
         typeof(string[]).GetFullNameCompilable().Must().Be("System.String[]");
   }

   public class Open_generic_type_definitions
   {
      public class with_one_type_parameter
      {
         [XF] public void returns_name_with_empty_angle_brackets() =>
            typeof(List<>).GetFullNameCompilable().Must().Be("System.Collections.Generic.List<>");

         [XF] public void works_for_nested_generic_types() =>
            typeof(GenericNested<>).GetFullNameCompilable().Must().Be("Compze.Internals.SystemCE.Tests.ReflectionCE.GetFullNameCompilable_.GenericNested<>");
      }

      public class with_two_type_parameters
      {
         [XF] public void returns_name_with_comma_between_angle_brackets() =>
            typeof(Dictionary<,>).GetFullNameCompilable().Must().Be("System.Collections.Generic.Dictionary<,>");

         [XF] public void works_for_nested_generic_types() =>
            typeof(TwoParameterGenericNested<,>).GetFullNameCompilable().Must().Be("Compze.Internals.SystemCE.Tests.ReflectionCE.GetFullNameCompilable_.TwoParameterGenericNested<,>");
      }
   }

   public class Constructed_generic_types
   {
      [XF] public void returns_name_with_type_arguments() =>
         typeof(List<string>).GetFullNameCompilable().Must().Be("System.Collections.Generic.List<System.String>");

      [XF] public void handles_two_type_arguments() =>
         typeof(Dictionary<string, int>).GetFullNameCompilable().Must().Be("System.Collections.Generic.Dictionary<System.String,System.Int32>");

      [XF] public void handles_nested_generics() =>
         typeof(List<Dictionary<string, int>>).GetFullNameCompilable().Must().Be("System.Collections.Generic.List<System.Collections.Generic.Dictionary<System.String,System.Int32>>");

      [XF] public void handles_generic_with_nested_type_argument() =>
         typeof(List<Nested>).GetFullNameCompilable().Must().Be("System.Collections.Generic.List<Compze.Internals.SystemCE.Tests.ReflectionCE.GetFullNameCompilable_.Nested>");
   }
}
