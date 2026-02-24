using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Functional.Specifications.Pipe;

public class _tap_method
{
   [XF] public void executes_side_effect_and_returns_original_value()
   {
      var sideEffect = "";
      var result = "hello"._tap(s => sideEffect = s);
      result.Must().Be("hello");
      sideEffect.Must().Be("hello");
   }

   [XF] public void returns_same_reference_for_reference_types()
   {
      var list = new System.Collections.Generic.List<int>();
      var result = list._tap(l => l.Add(42));
      ReferenceEquals(result, list).Must().BeTrue();
      result.Count.Must().Be(1);
   }
}
