using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Underscore.Specifications.Pipe;

public class _tap_method
{
   [XF] public void executes_the_side_effect_and_returns_the_original_value()
   {
      var sideEffect = "";
      var result = "hello"._tap(s => sideEffect = s);
      result.Must().Be("hello");
      sideEffect.Must().Be("hello");
   }

   [XF] public void returns_the_same_reference_for_reference_types()
   {
      var list = new List<int>();
      var result = list._tap(l => l.Add(42));
      ReferenceEquals(result, list).Must().BeTrue();
      result.Count.Must().Be(1);
   }
}
