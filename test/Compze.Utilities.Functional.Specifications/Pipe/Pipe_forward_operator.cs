using System.Globalization;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Utilities.Functional.Specifications.Pipe;

public class Pipe_forward_operator
{
   [XF] public void passes_value_to_function_and_returns_result() =>
      5._(x => x * 2).Must().Be(10);

   [XF] public void chains_multiple_transformations() =>
      "hello"._(s => s.ToUpperInvariant())._(s => s + "!").Must().Be("HELLO!");

   [XF] public void works_with_type_changing_transformations() =>
      42._(x => x.ToString(CultureInfo.InvariantCulture)).Must().Be("42");
}
