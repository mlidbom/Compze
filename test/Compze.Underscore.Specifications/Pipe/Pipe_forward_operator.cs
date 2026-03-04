using System.Globalization;
using Compze.Must;
using Compze.xUnit.BDD;

namespace Compze.Underscore.Specifications.Pipe;

public class Pipe_forward_operator
{
   [XF] public void passes_the_value_to_the_function_and_returns_its_result() =>
      5._(x => x * 2).Must().Be(10);

   [XF] public void chains_multiple_transformations_left_to_right() =>
      "hello"._(s => s.ToUpperInvariant())._(s => s + "!").Must().Be("HELLO!");

   [XF] public void supports_type_changing_transformations() =>
      42._(x => x.ToString(CultureInfo.InvariantCulture)).Must().Be("42");
}
