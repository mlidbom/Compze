using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

namespace Compze.Underscore.Specifications.Pipe;

public class _mutateAsync_method
{
   [XF] public async Task executes_the_async_mutation_and_returns_the_same_instance()
   {
      var list = new List<int>();
      var result = await list._mutateAsync(async l =>
      {
         await Task.Yield();
         l.Add(42);
      });
      ReferenceEquals(result, list).Must().BeTrue();
      result.Count.Must().Be(1);
   }
}
