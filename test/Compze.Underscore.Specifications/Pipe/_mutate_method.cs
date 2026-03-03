using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Underscore.Specifications.Pipe;

public class _mutate_method
{
   [XF] public void executes_the_mutation_and_returns_the_same_instance()
   {
      var list = new List<int>();
      var result = list._mutate(l => l.Add(42));
      ReferenceEquals(result, list).Must().BeTrue();
      result.Count.Must().Be(1);
   }
}
