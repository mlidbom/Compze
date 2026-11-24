using BenchmarkDotNet.Attributes;
using Compze.Threading;

namespace Compze.Tests.Benchmarks.Threading;

///<summary>What a <see cref="LazyCE{TValue}"/> costs: creating one and reading its value for the first time, and reading a value already created.</summary>
///<remarks>Code may create a <see cref="LazyCE{TValue}"/> for each of millions of objects, such as JAStudio's parser for each candidate word,<br/>
/// so both its size and the time to create and read one matter.</remarks>
[MemoryDiagnoser]
public class LazyCEBenchmarks
{
   static readonly object TheValue = new();
   static readonly Func<object> ReturnTheValue = () => TheValue;
   LazyCE<object> _alreadyCreated = null!;

   [GlobalSetup] public void CreateTheValueOfOneInstance()
   {
      _alreadyCreated = new LazyCE<object>(ReturnTheValue);
      _ = _alreadyCreated.Value;
   }

   [Benchmark] public object Creating_an_instance_and_reading_its_value() => new LazyCE<object>(ReturnTheValue).Value;

   [Benchmark] public object Reading_a_value_already_created() => _alreadyCreated.Value;
}
