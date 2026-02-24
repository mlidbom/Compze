using System.Threading.Tasks;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Functional.Specifications.Pipe;

public class _mutateAsync_method
{
   [XF] public async Task mutates_value_asynchronously_and_returns_it()
   {
      var list = new System.Collections.Generic.List<int>();
      var result = await list._mutateAsync(async l =>
      {
         await Task.Yield();
         l.Add(42);
      });
      ReferenceEquals(result, list).Must().BeTrue();
      result.Count.Must().Be(1);
   }
}
